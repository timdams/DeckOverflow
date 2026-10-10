-- Het beheerpaneel van de maker: feedback en scores lezen in het spel zelf (D-013).
-- Alleen wie app_metadata.role = 'superuser' heeft. Die rol zet alleen de service-sleutel (backend.md, Superuser),
-- en de controle gebeurt hier, niet in de browser. Feedback blijft voor iedereen anders onleesbaar.
-- Ontwerp: docs/wereld/backend.md (Beheer).

create function private.is_superuser() returns boolean
language sql stable set search_path = '' as $$
    select coalesce((select auth.jwt()) -> 'app_metadata' ->> 'role', '') = 'superuser';
$$;
revoke execute on function private.is_superuser() from public;
grant execute on function private.is_superuser() to authenticated;

-- Per onderwerp: hartjes, meh en hoeveel spelers iets lieten weten.
create function public.admin_feedback_summary()
returns table (department text, subject text, likes bigint, dislikes bigint, players bigint)
language plpgsql stable security definer set search_path = '' as $$
begin
    if not private.is_superuser() then
        raise exception 'alleen voor de maker' using errcode = '42501';
    end if;
    return query
        select f.department, f.subject,
               count(*) filter (where f.verdict = 'like'),
               count(*) filter (where f.verdict = 'dislike'),
               count(distinct f.user_id)
        from public.feedback f
        group by f.department, f.subject
        order by f.department, f.subject;
end;
$$;

-- De berichten, de nieuwste eerst. Zonder bijnaam: wat een speler schrijft, hangt niet aan wie hij is.
create function public.admin_feedback_comments(max_rows int default 200)
returns table (created_at timestamptz, department text, subject text, verdict public.feedback_verdict, comment text)
language plpgsql stable security definer set search_path = '' as $$
begin
    if not private.is_superuser() then
        raise exception 'alleen voor de maker' using errcode = '42501';
    end if;
    return query
        select f.created_at, f.department, f.subject, f.verdict, f.comment
        from public.feedback f
        where f.comment is not null
        order by f.created_at desc
        limit least(greatest(max_rows, 1), 1000);
end;
$$;

-- De scores, de nieuwste dag eerst, met bijnaam en de stand van de nachtelijke controle.
create function public.admin_scores(max_rows int default 200)
returns table (created_at timestamptz, nickname text, department text, seed_date date, claimed_score int, verified public.score_status)
language plpgsql stable security definer set search_path = '' as $$
begin
    if not private.is_superuser() then
        raise exception 'alleen voor de maker' using errcode = '42501';
    end if;
    return query
        select s.created_at, p.nickname, s.department, s.seed_date, s.claimed_score, s.verified
        from public.scores s
        left join public.profiles p on p.user_id = s.user_id
        order by s.seed_date desc, s.created_at desc
        limit least(greatest(max_rows, 1), 1000);
end;
$$;

revoke execute on function public.admin_feedback_summary() from public, anon;
revoke execute on function public.admin_feedback_comments(int) from public, anon;
revoke execute on function public.admin_scores(int) from public, anon;
grant execute on function public.admin_feedback_summary() to authenticated;
grant execute on function public.admin_feedback_comments(int) to authenticated;
grant execute on function public.admin_scores(int) to authenticated;

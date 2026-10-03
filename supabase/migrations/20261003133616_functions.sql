-- Functies die de shell aanroept via /rest/v1/rpc/<naam>. Alle security definer met een vast search_path;
-- elke functie controleert zelf wie er belt.

-- Klascodes: 6 tekens zonder verwarrende letters (geen 0/O, 1/I/L), 31^6 mogelijkheden.
create function private.random_class_code() returns text
language sql volatile set search_path = '' as $$
    select string_agg(substr('ABCDEFGHJKMNPQRSTUVWXYZ23456789', 1 + floor(random() * 31)::int, 1), '')
    from generate_series(1, 6);
$$;
revoke execute on function private.random_class_code() from public;

-- Een klas maken: alleen met een geregistreerd account, niet als gast. Geeft de code terug.
create function public.create_class(class_name text) returns text
language plpgsql security definer set search_path = '' as $$
declare
    new_code text;
begin
    if auth.uid() is null or coalesce((auth.jwt() ->> 'is_anonymous')::boolean, true) then
        raise exception 'Alleen een geregistreerd account maakt een klas' using errcode = '42501';
    end if;
    class_name := btrim(class_name);
    if class_name is null or length(class_name) not between 1 and 60 then
        raise exception 'Een klasnaam heeft 1 tot 60 tekens' using errcode = '22023';
    end if;

    for attempt in 1..20 loop
        new_code := private.random_class_code();
        begin
            insert into public.classes (code, name, owner_id) values (new_code, class_name, auth.uid());
            return new_code;
        exception when unique_violation then
            -- Code bestaat al: nog eens
        end;
    end loop;
    raise exception 'Geen vrije klascode gevonden';
end;
$$;

-- Bij een klas aansluiten met de code. Ook als gast. Geeft de id van de klas terug.
create function public.join_class(class_code text) returns uuid
language plpgsql security definer set search_path = '' as $$
declare
    found uuid;
begin
    if auth.uid() is null then
        raise exception 'Niet aangemeld' using errcode = '42501';
    end if;
    select c.id into found from public.classes c where c.code = upper(btrim(class_code));
    if found is null then
        raise exception 'Onbekende klascode' using errcode = 'P0002';
    end if;
    insert into public.class_members (class_id, user_id) values (found, auth.uid())
    on conflict do nothing;
    return found;
end;
$$;

-- Een afdeling vrijgeven voor de klas. Alleen de eigenaar. De client van een leerling leest
-- released_departments en schrijft zelf een unlock met how = teacher.
create function public.release_department(class uuid, department text) returns void
language plpgsql security definer set search_path = '' as $$
begin
    update public.classes c
    set released_departments = array_append(c.released_departments, department)
    where c.id = class and c.owner_id = auth.uid() and not (department = any (c.released_departments));

    if not found and not exists (select 1 from public.classes c where c.id = class and c.owner_id = auth.uid()) then
        raise exception 'Alleen de eigenaar geeft een afdeling vrij' using errcode = '42501';
    end if;
end;
$$;

-- Hoe deed iedereen het vandaag? Alleen aantallen per emmer, zonder namen. Per speler telt de beste
-- score die niet afgewezen werd.
create function public.score_histogram(department text, seed_date date, bucket_width int default 10)
returns table (bucket int, players bigint)
language sql stable security definer set search_path = '' as $$
    select (best / greatest(bucket_width, 1)) * greatest(bucket_width, 1) as bucket, count(*) as players
    from (
        select max(s.claimed_score) as best
        from public.scores s
        where s.department = score_histogram.department
          and s.seed_date = score_histogram.seed_date
          and s.verified <> 'rejected'
        group by s.user_id
    ) per_player
    group by 1
    order by 1;
$$;

revoke execute on function
    public.create_class(text),
    public.join_class(text),
    public.release_department(uuid, text),
    public.score_histogram(text, date, int)
from public, anon;

grant execute on function
    public.create_class(text),
    public.join_class(text),
    public.release_department(uuid, text),
    public.score_histogram(text, date, int)
to authenticated;

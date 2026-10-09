-- De Prikklok telt de eerste voltooide run van de dag, niet de beste (beslist op 3 oktober 2026; deze migratie: D-007).
-- Per speler telt de eerste score die niet afgewezen werd. create or replace houdt de rechten van de functie.
create or replace function public.score_histogram(department text, seed_date date, bucket_width int default 10)
returns table (bucket int, players bigint)
language sql stable security definer set search_path = '' as $$
    select (first_score / greatest(bucket_width, 1)) * greatest(bucket_width, 1) as bucket, count(*) as players
    from (
        select distinct on (s.user_id) s.claimed_score as first_score
        from public.scores s
        where s.department = score_histogram.department
          and s.seed_date = score_histogram.seed_date
          and s.verified <> 'rejected'
        order by s.user_id, s.created_at, s.id
    ) per_player
    group by 1
    order by 1;
$$;

-- Feedback in het spel: een hartje (of niet) per gevecht of puzzel, en een optioneel bericht na een run.
-- Spelers kunnen alleen schrijven, nooit lezen: ook hun eigen rijen niet. Alleen Tim leest, in het dashboard
-- of met de service-sleutel. Zo kan feedback nooit bij een klasgenoot of docent belanden.
-- Ontwerp: docs/wereld/backend.md (Feedback).

create type public.feedback_verdict as enum ('like', 'dislike');

create table public.feedback (
    id bigint generated always as identity primary key,
    user_id uuid not null default auth.uid() references auth.users (id) on delete cascade,
    -- De afdeling, bv. card-hall of control-room
    department text not null check (length(department) between 1 and 40),
    -- Waarover het gaat, bv. enemy:nameless, level:3 of run
    subject text not null check (length(subject) between 1 and 80),
    verdict public.feedback_verdict,
    -- Vrije tekst, alleen voor de maker. Het spel vraagt er geen namen in te zetten.
    comment text check (comment is null or length(comment) between 1 and 500),
    created_at timestamptz not null default now(),
    check (verdict is not null or comment is not null)
);
create index feedback_user_id_idx on public.feedback (user_id);
create index feedback_subject_idx on public.feedback (department, subject);

-- Alleen invullen wat de speler kiest; user_id en created_at vult de database zelf
revoke all on public.feedback from anon, authenticated;
grant insert (department, subject, verdict, comment) on public.feedback to authenticated;

alter table public.feedback enable row level security;

-- Schrijven als jezelf. Er is geen policy om te lezen: niemand leest feedback via de API.
create policy "feedback: indienen door jezelf" on public.feedback
for insert to authenticated
with check (user_id = (select auth.uid()));

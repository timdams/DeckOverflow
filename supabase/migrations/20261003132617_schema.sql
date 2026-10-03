-- Deck Overflow: tabellen, rechten en row level security.
-- Ontwerp: docs/spike-design-doc.md#hosting-accounts-en-data, uitvoering: supabaseplan.md (stap 3).
-- Alles hangt aan auth.users: wie zijn account verwijdert, verliest alles via cascade.

-- ---------- Types ----------

-- De volgorde telt: een toestand gaat nooit terug (zie keep_progress).
create type public.part_state as enum ('in_bag', 'unpacked', 'assembled');
create type public.unlock_how as enum ('boss', 'safety_net', 'teacher');
create type public.score_status as enum ('pending', 'ok', 'rejected');

-- Hulpfuncties voor de policies staan apart: niet bereikbaar via de REST-API.
create schema private;
grant usage on schema private to anon, authenticated;

-- ---------- Tabellen ----------

create table public.profiles (
    user_id uuid primary key references auth.users (id) on delete cascade,
    -- Gegenereerd uit woordenlijsten, nooit door de speler gekozen
    nickname text not null unique,
    created_at timestamptz not null default now()
);

create table public.progress (
    user_id uuid not null references auth.users (id) on delete cascade,
    -- Een onderdeel is een Codex-pagina, bv. int-truncation
    item text not null check (length(item) between 1 and 80),
    state public.part_state not null,
    -- De getallen van het eerste moment op de Codex-pagina
    "values" jsonb check ("values" is null or (jsonb_typeof("values") = 'object' and pg_column_size("values") < 4000)),
    updated_at timestamptz not null default now(),
    primary key (user_id, item)
);

create table public.unlocks (
    user_id uuid not null references auth.users (id) on delete cascade,
    department text not null check (length(department) between 1 and 40),
    how public.unlock_how not null,
    unlocked_at timestamptz not null default now(),
    primary key (user_id, department)
);

create table public.classes (
    id uuid primary key default gen_random_uuid(),
    -- 6 tekens zonder 0/O en 1/I/L, zie create_class
    code text not null unique,
    name text not null check (length(name) between 1 and 60),
    owner_id uuid not null references auth.users (id) on delete cascade,
    released_departments text[] not null default '{}',
    created_at timestamptz not null default now()
);
create index classes_owner_id_idx on public.classes (owner_id);

create table public.class_members (
    class_id uuid not null references public.classes (id) on delete cascade,
    user_id uuid not null references auth.users (id) on delete cascade,
    joined_at timestamptz not null default now(),
    primary key (class_id, user_id)
);
create index class_members_user_id_idx on public.class_members (user_id);

create table public.scores (
    id bigint generated always as identity primary key,
    user_id uuid not null default auth.uid() references auth.users (id) on delete cascade,
    department text not null check (length(department) between 1 and 40),
    seed_date date not null,
    -- De commandolijst van de run; de nachtelijke controle speelt ze opnieuw af
    commands jsonb not null check (jsonb_typeof(commands) = 'array' and pg_column_size(commands) < 200000),
    claimed_score int not null check (claimed_score >= 0),
    verified public.score_status not null default 'pending',
    created_at timestamptz not null default now()
);
create index scores_user_id_idx on public.scores (user_id);
create index scores_day_idx on public.scores (department, seed_date);

-- ---------- Hulpfuncties voor de policies ----------
-- security definer, zodat een policy niet recursief over class_members loopt.

create function private.is_member_of(class uuid) returns boolean
language sql stable security definer set search_path = '' as $$
    select exists (
        select 1 from public.class_members m
        where m.class_id = class and m.user_id = auth.uid()
    );
$$;

create function private.owns_class(class uuid) returns boolean
language sql stable security definer set search_path = '' as $$
    select exists (
        select 1 from public.classes c
        where c.id = class and c.owner_id = auth.uid()
    );
$$;

-- Zit de ander in een klas waar ik ook in zit?
create function private.shares_class(other uuid) returns boolean
language sql stable security definer set search_path = '' as $$
    select exists (
        select 1 from public.class_members mine
        join public.class_members theirs on theirs.class_id = mine.class_id
        where mine.user_id = auth.uid() and theirs.user_id = other
    );
$$;

-- Ben ik eigenaar van een klas waar de leerling in zit?
create function private.is_teacher_of(student uuid) returns boolean
language sql stable security definer set search_path = '' as $$
    select exists (
        select 1 from public.classes c
        join public.class_members m on m.class_id = c.id
        where c.owner_id = auth.uid() and m.user_id = student
    );
$$;

-- Gaf de docent van een van mijn klassen deze afdeling vrij?
create function private.released_to_me(department text) returns boolean
language sql stable security definer set search_path = '' as $$
    select exists (
        select 1 from public.class_members m
        join public.classes c on c.id = m.class_id
        where m.user_id = auth.uid() and department = any (c.released_departments)
    );
$$;

revoke execute on all functions in schema private from public;
grant execute on all functions in schema private to authenticated;

-- Een toestand gaat nooit terug, en de getallen van het eerste moment blijven.
create function private.keep_progress() returns trigger
language plpgsql set search_path = '' as $$
begin
    if new.state < old.state then
        new.state := old.state;
    end if;
    new."values" := coalesce(old."values", new."values");
    new.updated_at := greatest(old.updated_at, new.updated_at);
    return new;
end;
$$;

create trigger keep_progress before update on public.progress
for each row execute function private.keep_progress();

-- ---------- Rechten ----------
-- Zonder sessie (de rol anon) kan je niets; elke speler heeft minstens een gastsessie.
-- Wat alleen via een functie mag, krijgt geen schrijfrecht op de tabel.

revoke all on public.profiles, public.progress, public.unlocks, public.classes, public.class_members, public.scores from anon;

revoke insert, update, delete on public.profiles from authenticated;
revoke delete on public.progress from authenticated;
revoke update, delete on public.unlocks from authenticated;
revoke insert, update on public.classes from authenticated;
grant update (name, released_departments) on public.classes to authenticated;
revoke insert, update on public.class_members from authenticated;
revoke insert, update, delete on public.scores from authenticated;
grant insert (department, seed_date, commands, claimed_score) on public.scores to authenticated;

-- ---------- Row level security ----------

alter table public.profiles enable row level security;
alter table public.progress enable row level security;
alter table public.unlocks enable row level security;
alter table public.classes enable row level security;
alter table public.class_members enable row level security;
alter table public.scores enable row level security;

-- Je bijnaam zien jij, je klasgenoten en je docent. Je gebruikersnaam staat hier niet in.
create policy "profiles: jezelf, je klas, je docent" on public.profiles
for select to authenticated
using (user_id = (select auth.uid()) or private.shares_class(user_id) or private.is_teacher_of(user_id));

create policy "progress: lezen door jezelf en je docent" on public.progress
for select to authenticated
using (user_id = (select auth.uid()) or private.is_teacher_of(user_id));

create policy "progress: schrijven door jezelf" on public.progress
for insert to authenticated
with check (user_id = (select auth.uid()));

create policy "progress: bijwerken door jezelf" on public.progress
for update to authenticated
using (user_id = (select auth.uid()))
with check (user_id = (select auth.uid()));

create policy "unlocks: lezen door jezelf en je docent" on public.unlocks
for select to authenticated
using (user_id = (select auth.uid()) or private.is_teacher_of(user_id));

-- Een vrijgave door de docent schrijf je alleen als die docent ze echt vrijgaf.
create policy "unlocks: schrijven door jezelf" on public.unlocks
for insert to authenticated
with check (user_id = (select auth.uid()) and (how <> 'teacher' or private.released_to_me(department)));

create policy "classes: leden en eigenaar" on public.classes
for select to authenticated
using (owner_id = (select auth.uid()) or private.is_member_of(id));

create policy "classes: eigenaar past aan" on public.classes
for update to authenticated
using (owner_id = (select auth.uid()))
with check (owner_id = (select auth.uid()));

create policy "classes: eigenaar verwijdert" on public.classes
for delete to authenticated
using (owner_id = (select auth.uid()));

create policy "class_members: leden en eigenaar" on public.class_members
for select to authenticated
using (user_id = (select auth.uid()) or private.is_member_of(class_id) or private.owns_class(class_id));

-- Zelf vertrekken mag; de docent mag iemand uit de klas halen.
create policy "class_members: vertrekken" on public.class_members
for delete to authenticated
using (user_id = (select auth.uid()) or private.owns_class(class_id));

create policy "scores: jezelf, je klas, je docent" on public.scores
for select to authenticated
using (user_id = (select auth.uid()) or private.shares_class(user_id) or private.is_teacher_of(user_id));

-- Alleen voor vandaag of gisteren (een run die over middernacht UTC loopt). verified blijft pending:
-- die kolom kan de client niet schrijven.
create policy "scores: indienen door jezelf" on public.scores
for insert to authenticated
with check (user_id = (select auth.uid()) and seed_date between current_date - 1 and current_date);

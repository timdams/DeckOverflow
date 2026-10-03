-- Bijnamen: elk account krijgt er een, gegenereerd uit twee woordenlijsten en een getal ("Rusty Bolt 42").
-- Vrije tekst die anderen zien, bestaat niet. De lijsten staan ook in supabaseplan.md, met de woorden
-- die bewust weggelaten zijn. 50 x 50 x 90 = 225.000 combinaties.

create function private.random_nickname() returns text
language sql volatile set search_path = '' as $$
    select
        (array[
            'Rusty', 'Shiny', 'Brass', 'Copper', 'Steel', 'Silver', 'Golden', 'Humming', 'Ticking', 'Whirring',
            'Clicking', 'Buzzing', 'Spinning', 'Rolling', 'Sturdy', 'Steady', 'Nimble', 'Swift', 'Quiet', 'Patient',
            'Clever', 'Careful', 'Bright', 'Polished', 'Oiled', 'Tidy', 'Trusty', 'Lucky', 'Sparky', 'Bouncy',
            'Cosy', 'Gentle', 'Brave', 'Bold', 'Calm', 'Eager', 'Merry', 'Jolly', 'Curious', 'Tiny',
            'Mighty', 'Little', 'Square', 'Round', 'Striped', 'Dotted', 'Folded', 'Paper', 'Iron', 'Cobalt'
        ])[1 + floor(random() * 50)::int]
        || ' ' ||
        (array[
            'Bolt', 'Gear', 'Cog', 'Sprocket', 'Spring', 'Lever', 'Pulley', 'Wrench', 'Hammer', 'Spanner',
            'Rivet', 'Washer', 'Hinge', 'Valve', 'Gauge', 'Dial', 'Switch', 'Fuse', 'Magnet', 'Piston',
            'Spindle', 'Wheel', 'Axle', 'Anvil', 'Kettle', 'Ladle', 'Funnel', 'Crate', 'Pallet', 'Conveyor',
            'Robot', 'Engine', 'Turbine', 'Boiler', 'Chimney', 'Whistle', 'Clock', 'Compass', 'Ruler', 'Pencil',
            'Stapler', 'Lantern', 'Ladder', 'Bucket', 'Barrel', 'Teapot', 'Byte', 'Widget', 'Gadget', 'Gizmo'
        ])[1 + floor(random() * 50)::int]
        || ' ' ||
        (10 + floor(random() * 90)::int)::text;
$$;

-- Bij elk nieuw account, ook een gastaccount: een profiel met een vrije bijnaam.
create function private.handle_new_user() returns trigger
language plpgsql security definer set search_path = '' as $$
begin
    for attempt in 1..50 loop
        begin
            insert into public.profiles (user_id, nickname) values (new.id, private.random_nickname());
            return new;
        exception when unique_violation then
            -- Botsing met een bestaande bijnaam: nog eens
        end;
    end loop;
    raise exception 'Geen vrije bijnaam gevonden na 50 pogingen';
end;
$$;

create trigger on_auth_user_created after insert on auth.users
for each row execute function private.handle_new_user();

revoke execute on function private.random_nickname(), private.handle_new_user() from public;

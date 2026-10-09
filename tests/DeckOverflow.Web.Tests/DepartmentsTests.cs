using DeckOverflow.Web.Backend;
using DeckOverflow.Web.Progress;
using DeckOverflow.Web.World;

namespace DeckOverflow.Web.Tests;

/// <summary>
/// Wie waar binnen mag op de plattegrond: vrijgegeven, binnenkort of ooit, met of zonder sleutel, en de superuser.
/// </summary>
public class DepartmentsTests
{
    private static readonly IReadOnlyDictionary<string, Unlock> NoKeys = new Dictionary<string, Unlock>();

    private static IReadOnlyDictionary<string, Unlock> KeyFor(string department) =>
        new Dictionary<string, Unlock> { [department] = new(UnlockHow.Boss, DateTimeOffset.UnixEpoch) };

    [Fact]
    public void De_Kaartenhal_staat_altijd_open()
    {
        var hall = Departments.Get(Departments.CardHall);

        Assert.True(Departments.IsEarned(hall, NoKeys));
        Assert.True(Departments.IsOpen(hall, NoKeys));
    }

    [Fact]
    public void Binnenkort_blijft_dicht_ook_met_de_sleutel()
    {
        var belt = Departments.Get(Departments.ConveyorBelt);
        Assert.Equal(Availability.Soon, belt.Availability);

        Assert.True(Departments.IsEarned(belt, KeyFor(Departments.ConveyorBelt)));
        Assert.False(Departments.IsOpen(belt, KeyFor(Departments.ConveyorBelt)));
    }

    [Fact]
    public void De_Controlekamer_gaat_open_met_de_sleutel_van_de_Kaartenhal()
    {
        var room = Departments.Get(Departments.ControlRoom);

        Assert.False(Departments.IsOpen(room, NoKeys));
        Assert.True(Departments.IsOpen(room, KeyFor(Departments.ControlRoom)));
    }

    [Fact]
    public void Vrijgegeven_gaat_open_zodra_je_de_sleutel_hebt()
    {
        var released = new Department("test-room", 5, [5], Availability.Released);

        Assert.False(Departments.IsOpen(released, NoKeys));
        Assert.True(Departments.IsOpen(released, KeyFor("test-room")));
    }

    [Fact]
    public void De_superuser_mag_in_alles_wat_gebouwd_is_maar_niet_in_wat_nog_in_de_doos_zit()
    {
        Assert.All(Departments.All, d =>
            Assert.Equal(d.Availability != Availability.Someday, Departments.IsOpen(d, NoKeys, superuser: true)));
    }

    [Fact]
    public void Wat_nog_in_de_doos_zit_heeft_geen_pagina()
    {
        Assert.All(Departments.All.Where(d => d.Availability == Availability.Someday), d => Assert.Null(d.Url));
        Assert.All(Departments.All.Where(d => d.Availability == Availability.Soon), d => Assert.NotNull(d.Url));
    }

    [Fact]
    public void Elke_afdeling_staat_er_een_keer_in_de_volgorde_van_het_boek()
    {
        Assert.Equal(Departments.All.Count, Departments.All.Select(d => d.Key).Distinct().Count());
        Assert.Equal(Departments.All.OrderBy(d => d.Step).Select(d => d.Key), Departments.All.Select(d => d.Key));
    }

    [Theory]
    [InlineData("http://localhost:5317/", true)]
    [InlineData("http://127.0.0.1:8080/", true)]
    [InlineData("http://[::1]:5000/", true)]
    [InlineData("https://timdams.github.io/DeckOverflow/", false)]
    [InlineData("https://localhost.example.com/", false)]
    public void Lokaal_is_iedereen_superuser_online_niet(string baseUri, bool local) =>
        Assert.Equal(local, Superuser.IsLocal(baseUri));
}

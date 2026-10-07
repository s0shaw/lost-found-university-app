namespace UniversityLostFound.Infrastructure.Persistence.Seed;

internal static class SeedData
{
    internal static readonly (string Name, bool IsHandoverPoint)[] Locations =
    [
        ("Main Reception", true),
        ("East Gate Security", true),
        ("Library Info Desk", true),
        ("Cafeteria", false),
        ("Library Reading Hall", false),
        ("Open Workspace — Floor 2", false),
        ("Open Workspace — Floor 3", false),
        ("Meeting Rooms — Floor 2", false),
        ("Sports Hall & Locker Rooms", false),
        ("Event Hall", false),
        ("Garden & Outdoor Area", false),
        ("Parking & Shuttle Stop", false),
    ];

    internal static readonly string[] ColourOptions =
        ["Black", "White or grey", "Blue", "Red", "Green", "Brown or tan", "Multi-coloured", "Don't know"];

    internal static readonly string[] SizeOptions =
        ["Small (pocket-sized)", "Medium", "Large", "Don't know"];

    internal static readonly string[] MarkOptions =
        ["Sticker", "Engraving or scratch", "Name tag", "None", "Don't know"];

    internal static readonly (string Name, string? Question, string[] Options)[] Categories =
    [
        ("Electronics", "Did it have a case or cover?", ["Yes", "No", "Don't know"]),
        ("Wallet & Cards", "Were there cards inside?", ["Yes", "No", "Don't know"]),
        ("Bag & Luggage", "What type of bag?", ["Backpack", "Handbag", "Shoulder bag", "Don't know"]),
        ("Keys", "How many keys on it?", ["1", "2-3", "4 or more", "Don't know"]),
        ("Clothing & Accessories", "What kind of item?",
            ["Glasses", "Scarf", "Hat", "Jewellery", "Other", "Don't know"]),
        ("Books & Stationery", "Was a name written inside?", ["Yes", "No", "Don't know"]),
        ("Other", null, []),
    ];

    internal static readonly (string UniversityId, string FullName)[] Members =
    [
        ("UM-204718", "Derya Aksoy"),
        ("UM-916035", "Kerem Altun"),
        ("UM-482915", "Elif Yalçın"),
        ("UM-730164", "Mert Doğan"),
        ("UM-358027", "Zeynep Arslan"),
        ("UM-641293", "Baran Yıldız"),
        ("UM-517840", "Ayşe Korkmaz"),
        ("UM-209463", "Tolga Şahin"),
        ("UM-873510", "Nihan Özkan"),
        ("UM-460782", "Cem Erdem"),
        ("UM-135629", "Selin Kaya"),
        ("UM-798204", "Onur Demirci"),
        ("UM-062418", "Pelin Aydın"),
        ("UM-541970", "Burak Çetin"),
        ("UM-327651", "Gizem Polat"),
        ("UM-684309", "Emre Taş"),
    ];

    internal static readonly (string UniversityId, string FullName, int ValidFromDays, int ValidUntilDays)[] Visitors =
    [
        ("UM-950173", "Lara Novak", -1, 7),
        ("UM-418605", "Tomas Fischer", -2, 5),
        ("UM-273948", "Maya Lindgren", 0, 14),
        ("UM-806521", "Hugo Ferreira", -60, -30),
    ];

    // Fictional default, overridden in every real deployment by Seed__StaffPassword.
    internal const string StaffPassword = "staffdemo123";

    internal static readonly string[] StaffUniversityIds = ["UM-204718", "UM-916035"];
}

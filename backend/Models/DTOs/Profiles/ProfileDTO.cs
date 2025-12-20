namespace RusalProject.Models.DTOs.Profiles;

public class ProfileDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ProfilePageDTO Page { get; set; } = new();
    public Dictionary<string, object> Entities { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ProfilePageDTO
{
    public string Size { get; set; } = "A4";
    public string Orientation { get; set; } = "portrait";
    public ProfileMarginsDTO Margins { get; set; } = new();
    public ProfilePageNumbersDTO? PageNumbers { get; set; }
}

public class ProfileMarginsDTO
{
    public int Top { get; set; }
    public int Right { get; set; }
    public int Bottom { get; set; }
    public int Left { get; set; }
}

public class ProfilePageNumbersDTO
{
    public bool Enabled { get; set; }
    public string Position { get; set; } = "bottom";
    public string Align { get; set; } = "center";
    public string Format { get; set; } = "{n}";
    public int? FontSize { get; set; }
    public string? FontStyle { get; set; }
    public string? FontFamily { get; set; }
    public int? MarginTop { get; set; }
    public int? MarginBottom { get; set; }
}

using CustomJsonSerializer.Exceptions;
using CustomJsonSerializer.Parsing;
using CustomJsonSerializer.Serialization;

User user = new User
{
    ID = 1,
    Name = "John Doe",
    IsActive = true
};

string json = JsonSerializer.Serialize(user);
string jsonString = JsonSerializer.Serialize("Hello, World!");
//Console.WriteLine(jsonString);

Console.WriteLine(json);

JsonParser parser = new("null");

object? result = parser.Parse();

Console.WriteLine(result is not null);

string jsonDe = """
{
    "ID": 12,
    "Name": "John",
    "IsActive": false
}
""";

User userDe = JsonSerializer.Deserialize<User>(jsonDe);

Console.WriteLine($"ID: {userDe.ID}, Name: {userDe.Name}, IsActive: {userDe.IsActive}");

User userRole = new()
{
    ID = 1,
    Name = "John",
    IsActive = true,
    Role = UserRole.Admin
};

string jsonEnum =
    JsonSerializer.Serialize(userRole);

Console.WriteLine(jsonEnum);

string jsonDeserialize = """
{
    "Id": 10,
    "Name": "John",
    "IsActive": true
}
""";

User userDeSerialize = JsonSerializer.Deserialize<User>(json)!;

Console.WriteLine(user.ID);
Console.WriteLine(user.Name);
Console.WriteLine(user.IsActive);

public class User
{
    public int ID { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public UserRole Role { get; set; }
}

public enum UserRole
{
    Admin,
    User,
    Guest
}



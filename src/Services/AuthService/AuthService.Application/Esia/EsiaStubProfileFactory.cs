using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AuthService.Application.DTOs;
using AuthService.Application.Helpers;

namespace AuthService.Application.Esia;

public static class EsiaStubProfileFactory
{
    public const string DevPassword = "GosuslugiDev1!";

    private static readonly string[] Streets =
    [
        "Тверская", "Арбат", "Новый Арбат", "Ленинский проспект", "Профсоюзная",
        "Садовая-Кудринская", "Пятницкая", "Покровка"
    ];

    public static string SubjectIdForPhone(string phone) =>
        "stub:" + PhoneNormalizer.Normalize(phone);

    public static EsiaUserInfo ForRegister(
        string lastName,
        string firstName,
        string? middleName,
        string email,
        string phone)
    {
        var normalized = PhoneNormalizer.Normalize(phone);
        var profile = Generate(normalized);
        profile.LastName = lastName.Trim();
        profile.FirstName = firstName.Trim();
        profile.MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim();
        profile.Email = email.Trim();
        profile.Phone = normalized;
        return profile;
    }

    /// <summary>
    /// Уже существующий аккаунт (вход через Госуслуги или привязка):
    /// ФИО, почта, дата рождения и пол не трогаем.
    /// </summary>
    public static EsiaUserInfo ForExistingAccount(string phone)
    {
        var normalized = PhoneNormalizer.Normalize(phone);
        var profile = Generate(normalized);
        profile.FirstName = null;
        profile.LastName = null;
        profile.MiddleName = null;
        profile.Email = null;
        profile.BirthDate = null;
        profile.Gender = null;
        profile.Phone = normalized;
        return profile;
    }

    public static EsiaUserInfo ForLink(string phone) => ForExistingAccount(phone);

    public static EsiaUserInfo Generate(string normalizedPhone)
    {
        var seed = BitConverter.ToInt32(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedPhone)), 0);
        var rnd = new Random(seed);
        var digits = new string(normalizedPhone.Where(char.IsDigit).ToArray());
        if (digits.Length < 10)
            digits = digits.PadLeft(10, '0');

        var oms = ("77" + digits.PadRight(14, '0'))[..16];
        var snils = BuildSnils(rnd);
        var street = Streets[Math.Abs(seed) % Streets.Length];
        var house = (Math.Abs(seed) % 80 + 1).ToString(CultureInfo.InvariantCulture);
        var flat = (Math.Abs(seed) % 200 + 1).ToString(CultureInfo.InvariantCulture);
        var gender = Math.Abs(seed) % 2 == 0 ? "M" : "F";
        var birth = DateTime.SpecifyKind(
            new DateTime(1975, 1, 1).AddDays(Math.Abs(seed) % (40 * 365)),
            DateTimeKind.Utc);

        var residence = new EsiaAddressInfo
        {
            Type = "PLV",
            PostCode = "125009",
            Country = "RUS",
            Region = "Москва",
            City = "Москва",
            Street = street,
            House = house,
            Flat = flat,
            AddressStr = $"г. Москва, ул. {street}, д. {house}, кв. {flat}"
        };

        var registration = new EsiaAddressInfo
        {
            Type = "PRG",
            PostCode = residence.PostCode,
            Country = residence.Country,
            Region = residence.Region,
            City = residence.City,
            Street = residence.Street,
            House = residence.House,
            Flat = residence.Flat,
            AddressStr = residence.AddressStr
        };

        return new EsiaUserInfo
        {
            SubjectId = SubjectIdForPhone(normalizedPhone),
            Phone = normalizedPhone,
            Snils = snils,
            OmsNumber = oms,
            BirthDate = birth,
            Gender = gender,
            ResidenceAddress = residence,
            RegistrationAddress = registration,
            MedicalDocuments =
            [
                new EsiaDocumentInfo
                {
                    Type = "MDCL_PLCY",
                    Number = oms,
                    IssueDate = birth.AddYears(18).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                    IssuedBy = "Московский фонд ОМС (заглушка)"
                }
            ]
        };
    }

    private static string BuildSnils(Random rnd)
    {
        var body = rnd.Next(100_000_000, 999_999_999);
        var checksum = rnd.Next(0, 100);
        return $"{body:000000000}{checksum:00}";
    }
}

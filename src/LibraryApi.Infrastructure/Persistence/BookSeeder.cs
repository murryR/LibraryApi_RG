using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LibraryApi.Domain.Entities;

namespace LibraryApi.Infrastructure.Persistence;

public class BookSeeder
{
    private readonly AppDbContext _context;
    private readonly ILogger<BookSeeder> _logger;

    public BookSeeder(AppDbContext context, ILogger<BookSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Calculates the ISBN-13 check digit based on the first 12 digits.
    /// ISBN-13 uses EAN-13 algorithm: Multiply each digit by 1 or 3 alternately, sum them, find remainder mod 10, check digit = 10 - remainder.
    /// </summary>
    private static string CalculateIsbn13CheckDigit(string isbn12)
    {
        if (isbn12.Length != 12)
            throw new ArgumentException("ISBN-12 must be exactly 12 digits");

        var sum = 0;
        for (int i = 0; i < 12; i++)
        {
            // Positions start at 1 for EAN-13: position 1 = weight 1, position 2 = weight 3, etc.
            var weight = (i % 2 == 0) ? 1 : 3;
            sum += int.Parse(isbn12[i].ToString()) * weight;
        }

        var checkDigit = (10 - (sum % 10)) % 10;
        return checkDigit.ToString();
    }

    /// <summary>
    /// Generates a valid ISBN-13 with proper check digit calculation.
    /// Format: 978-XX-YYYY-ZZZZZ-C where C is the calculated check digit.
    /// </summary>
    private static string GenerateValidIsbn13(string prefix, string middleDigits)
    {
        // Remove hyphens for calculation
        var isbn12 = $"{prefix}{middleDigits}";
        isbn12 = isbn12.Replace("-", "");
        
        if (isbn12.Length != 12)
            throw new ArgumentException($"Combined prefix and middle digits must be 12 characters, got: {isbn12.Length}");

        var checkDigit = CalculateIsbn13CheckDigit(isbn12);
        
        // Format ISBN-13 with hyphens: 978-XX-XXXXXXX-X
        var fullIsbn = isbn12 + checkDigit;
        if (fullIsbn.Length == 13)
        {
            return $"{fullIsbn.Substring(0, 3)}-{fullIsbn.Substring(3, 2)}-{fullIsbn.Substring(5, 7)}-{fullIsbn.Substring(12, 1)}";
        }
        
        // Fallback: return without formatting if length is unexpected
        return fullIsbn;
    }

    public async Task SeedBooksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if books already exist
            if (await _context.Books.AnyAsync(cancellationToken))
            {
                _logger.LogInformation("Books already exist in database. Skipping seed.");
                return;
            }

            var books = new List<Book>
            {
                // 50 knih – české názvy (UTF-8)
                new Book { Id = Guid.NewGuid().ToString(), Name = "Osudová noc", Author = "Eva Nováková", IssueYear = 2018, ISBN = GenerateValidIsbn13("978", "802490001"), NumberOfPieces = 12 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Řeka stínů", Author = "Jan Černý", IssueYear = 2019, ISBN = GenerateValidIsbn13("978", "802490002"), NumberOfPieces = 8 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Dům za mlhou", Author = "Marie Svobodová", IssueYear = 2017, ISBN = GenerateValidIsbn13("978", "802490003"), NumberOfPieces = 15 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Cesta do neznáma", Author = "Petr Dvořák", IssueYear = 2020, ISBN = GenerateValidIsbn13("978", "802490004"), NumberOfPieces = 11 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Poslední léto", Author = "Alena Veselá", IssueYear = 2016, ISBN = GenerateValidIsbn13("978", "802490005"), NumberOfPieces = 19 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Ztracený dopis", Author = "Tomáš Horák", IssueYear = 2021, ISBN = GenerateValidIsbn13("978", "802490006"), NumberOfPieces = 7 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Mlčení lesa", Author = "Lucie Králová", IssueYear = 2015, ISBN = GenerateValidIsbn13("978", "802490007"), NumberOfPieces = 14 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Čas vlků", Author = "Martin Procházka", IssueYear = 2019, ISBN = GenerateValidIsbn13("978", "802490008"), NumberOfPieces = 10 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Křížová cesta", Author = "Jana Malá", IssueYear = 2018, ISBN = GenerateValidIsbn13("978", "802490009"), NumberOfPieces = 6 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Podzimní vítr", Author = "Ondřej Němec", IssueYear = 2017, ISBN = GenerateValidIsbn13("978", "802490010"), NumberOfPieces = 22 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Stříbrný úsvit", Author = "Kateřina Holubová", IssueYear = 2020, ISBN = GenerateValidIsbn13("978", "802490011"), NumberOfPieces = 9 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Život v zahradě", Author = "Pavel Šimek", IssueYear = 2016, ISBN = GenerateValidIsbn13("978", "802490012"), NumberOfPieces = 18 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Tajemství věže", Author = "Lenka Marková", IssueYear = 2021, ISBN = GenerateValidIsbn13("978", "802490013"), NumberOfPieces = 13 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Dědictví otců", Author = "Václav Novotný", IssueYear = 2014, ISBN = GenerateValidIsbn13("978", "802490014"), NumberOfPieces = 11 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Prsten a meč", Author = "Ivana Horáková", IssueYear = 2019, ISBN = GenerateValidIsbn13("978", "802490015"), NumberOfPieces = 16 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Bílý kůň", Author = "Radek Polák", IssueYear = 2018, ISBN = GenerateValidIsbn13("978", "802490016"), NumberOfPieces = 20 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Večerní zpěv", Author = "Simona Tichá", IssueYear = 2017, ISBN = GenerateValidIsbn13("978", "802490017"), NumberOfPieces = 8 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Kamenný most", Author = "David Kratochvíl", IssueYear = 2020, ISBN = GenerateValidIsbn13("978", "802490018"), NumberOfPieces = 14 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Černé jezero", Author = "Tereza Bártová", IssueYear = 2015, ISBN = GenerateValidIsbn13("978", "802490019"), NumberOfPieces = 17 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Hory a údolí", Author = "Jakub Zeman", IssueYear = 2016, ISBN = GenerateValidIsbn13("978", "802490020"), NumberOfPieces = 12 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Sníh na zápraží", Author = "Michaela Fialová", IssueYear = 2021, ISBN = GenerateValidIsbn13("978", "802490021"), NumberOfPieces = 9 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Dívka z údolí", Author = "Filip Urban", IssueYear = 2019, ISBN = GenerateValidIsbn13("978", "802490022"), NumberOfPieces = 21 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Řemeslo smrti", Author = "Barbora Kovářová", IssueYear = 2018, ISBN = GenerateValidIsbn13("978", "802490023"), NumberOfPieces = 5 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Zlomená křídla", Author = "Adam Veselý", IssueYear = 2017, ISBN = GenerateValidIsbn13("978", "802490024"), NumberOfPieces = 15 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Pramen života", Author = "Kristýna Nová", IssueYear = 2020, ISBN = GenerateValidIsbn13("978", "802490025"), NumberOfPieces = 10 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Stará škola", Author = "Marek Doležal", IssueYear = 2014, ISBN = GenerateValidIsbn13("978", "802490026"), NumberOfPieces = 19 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Věčné jaro", Author = "Nikola Sedláčková", IssueYear = 2016, ISBN = GenerateValidIsbn13("978", "802490027"), NumberOfPieces = 7 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Hlubina ticha", Author = "Roman Bureš", IssueYear = 2021, ISBN = GenerateValidIsbn13("978", "802490028"), NumberOfPieces = 13 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Krajina snů", Author = "Veronika Černá", IssueYear = 2015, ISBN = GenerateValidIsbn13("978", "802490029"), NumberOfPieces = 16 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Poslední západ slunce", Author = "Daniel Málek", IssueYear = 2019, ISBN = GenerateValidIsbn13("978", "802490030"), NumberOfPieces = 11 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Šepot v rákosí", Author = "Hana Jandová", IssueYear = 2018, ISBN = GenerateValidIsbn13("978", "802490031"), NumberOfPieces = 8 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Drahokam v blátě", Author = "Libor Soukup", IssueYear = 2017, ISBN = GenerateValidIsbn13("978", "802490032"), NumberOfPieces = 14 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Půlnoc v mlýně", Author = "Zuzana Pavlíková", IssueYear = 2020, ISBN = GenerateValidIsbn13("978", "802490033"), NumberOfPieces = 6 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Řeka času", Author = "Patrik Hruška", IssueYear = 2016, ISBN = GenerateValidIsbn13("978", "802490034"), NumberOfPieces = 22 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Tvář ve tmě", Author = "Monika Vlčková", IssueYear = 2021, ISBN = GenerateValidIsbn13("978", "802490035"), NumberOfPieces = 9 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Vůně deště", Author = "Jiří Beran", IssueYear = 2015, ISBN = GenerateValidIsbn13("978", "802490036"), NumberOfPieces = 17 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Kámen a voda", Author = "Andrea Křížová", IssueYear = 2019, ISBN = GenerateValidIsbn13("978", "802490037"), NumberOfPieces = 12 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Zimní královna", Author = "Stanislav Růžička", IssueYear = 2018, ISBN = GenerateValidIsbn13("978", "802490038"), NumberOfPieces = 10 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Stopy v písku", Author = "Martina Havlíčková", IssueYear = 2017, ISBN = GenerateValidIsbn13("978", "802490039"), NumberOfPieces = 15 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Červený útes", Author = "Lukáš Vacek", IssueYear = 2014, ISBN = GenerateValidIsbn13("978", "802490040"), NumberOfPieces = 8 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Světlo za okny", Author = "Petra Kočí", IssueYear = 2020, ISBN = GenerateValidIsbn13("978", "802490041"), NumberOfPieces = 20 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Hodina duchů", Author = "Michal Strnad", IssueYear = 2016, ISBN = GenerateValidIsbn13("978", "802490042"), NumberOfPieces = 11 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Noc plná hvězd", Author = "Eliška Nováková", IssueYear = 2021, ISBN = GenerateValidIsbn13("978", "802490043"), NumberOfPieces = 7 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Zrcadlo minulosti", Author = "Vojtěch Kučera", IssueYear = 2015, ISBN = GenerateValidIsbn13("978", "802490044"), NumberOfPieces = 13 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Píseň pro sestru", Author = "Aneta Horváthová", IssueYear = 2019, ISBN = GenerateValidIsbn13("978", "802490045"), NumberOfPieces = 16 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Trní a růže", Author = "Matěj Beneš", IssueYear = 2018, ISBN = GenerateValidIsbn13("978", "802490046"), NumberOfPieces = 14 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Dlouhá cesta domů", Author = "Karolína Dušková", IssueYear = 2017, ISBN = GenerateValidIsbn13("978", "802490047"), NumberOfPieces = 9 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Měsíc nad polem", Author = "Ondřej Čech", IssueYear = 2020, ISBN = GenerateValidIsbn13("978", "802490048"), NumberOfPieces = 18 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Prázdný trůn", Author = "Renata Štěpánková", IssueYear = 2016, ISBN = GenerateValidIsbn13("978", "802490049"), NumberOfPieces = 6 },
                new Book { Id = Guid.NewGuid().ToString(), Name = "Větrný mlýn", Author = "Tomáš Jelínek", IssueYear = 2021, ISBN = GenerateValidIsbn13("978", "802490050"), NumberOfPieces = 12 }
            };

            await _context.Books.AddRangeAsync(books, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Seeded {Count} books successfully", books.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding books");
            throw;
        }
    }
}



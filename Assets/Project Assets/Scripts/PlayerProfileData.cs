using System;
using UnityEngine;

[Serializable]
public class PlayerProfileData
{
    public string description;
    public string personalStatus;
    public PlayerBirthDate birthDate;
    public int iconIndex;
    public string gameVersion;
}

[Serializable]
public class PlayerBirthDate
{
    public int day;
    public int month;
    public int year;

    public bool IsValid()
    {
        try
        {
            // Crear DateTime para validar (usa año 2000 si el año no es válido para la validación)
            int validationYear = year >= 1900 && year <= DateTime.Now.Year ? year : 2000;
            DateTime testDate = new DateTime(validationYear, month, day);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public override string ToString()
    {
        return $"{day:D2}/{month:D2}/{year}";
    }

    public static bool TryParseFromStrings(string dayStr, string monthStr, string yearStr, out PlayerBirthDate birthDate)
    {
        birthDate = new PlayerBirthDate();

        // Permitir tanto "1" como "01"
        if (!int.TryParse(dayStr, out int day)) return false;
        if (!int.TryParse(monthStr, out int month)) return false;
        if (!int.TryParse(yearStr, out int year)) return false;

        birthDate.day = day;
        birthDate.month = month;
        birthDate.year = year;

        return birthDate.IsValid();
    }
}
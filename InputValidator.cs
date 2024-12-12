using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
public static class InputValidator {
    public static bool IsNumber(string input) =>
        Regex.IsMatch(input, @"^\d+$");
    public static bool IsLetter(string input) =>
        Regex.IsMatch(input, @"^[a-zA-Zа-яА-Я]+$");
    public static bool IsValidFolderPath(string input) {
        try {
            var fullPath = Path.GetFullPath(input);
            return !string.IsNullOrWhiteSpace(fullPath) &&
                   !Path.GetInvalidPathChars().Any(input.Contains);
        }
        catch (Exception) {
            return false;
        }
    }

    public static bool IsUnique(string input, IEnumerable<string> collection) =>
        !collection.Contains(input);
    public static bool MatchesPattern(string input, string pattern) =>
        Regex.IsMatch(input, pattern);
}

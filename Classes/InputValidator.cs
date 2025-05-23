﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using System.Windows.Media;
namespace DataValidation {
    public static class InputValidator {
        public static bool IsNumber(string input,object Sender = null) {
            var match = Regex.IsMatch(input, @"^\d+$");
            SetUnderline(match, Sender);
            return match;
        }

        public static bool IsLetter(string input, object Sender = null) {
            bool match = Regex.IsMatch(input, @"^[a-zA-Zа-яА-Я]+$");
            SetUnderline(match, Sender);
            return match;
        }
        public static bool IsFolderExists(string input, object Sender = null) {
            var IsPathExists = Path.Exists(input);
            SetUnderline(IsPathExists, Sender);
            return IsPathExists;
        }

        public static void SetUnderline(bool CheckResult, object Sender) {
            if (Sender is TextBox textBox) {
                var palette = new PaletteHelper().GetTheme();
                var color = CheckResult ? new SolidColorBrush(MaterialDesignColors.Recommended.GreenSwatch.Green300) : new SolidColorBrush(MaterialDesignColors.Recommended.RedSwatch.Red300);
                TextFieldAssist.SetUnderlineBrush(textBox, color);
            }
        }

        public static bool IsUnique(string input, IEnumerable<string> collection) =>
            !collection.Contains(input);
        public static bool MatchesPattern(string input, string pattern, object Sender) {
            var IsMatch = Regex.IsMatch(input, pattern);
            SetUnderline(IsMatch, Sender);
            return IsMatch;
        }
        public static bool MatchesExpectedBool(bool input,bool expected, object Sender) {
            SetUnderline(input == expected, Sender);
            return input == expected;
        }
    }
}

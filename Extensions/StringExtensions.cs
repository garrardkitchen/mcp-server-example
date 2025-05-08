public static class StringExtensions
{
    /// <summary>
    /// Converts each word in a string to have its first character capitalized and the rest in lowercase.
    /// </summary>
    /// <param name="input">The input string to convert.</param>
    /// <returns>A new string with each word capitalized, or the original string if it is null or empty.</returns>
    public static string ToCapitalize(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return string.Join(" ",
            input.Split(' ')
                .Select(word => word.Length > 0
                    ? char.ToUpper(word[0]) + (word.Length > 1 ? word.Substring(1).ToLower() : string.Empty)
                    : word));
    }
}
public static class StringExtensions
{
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
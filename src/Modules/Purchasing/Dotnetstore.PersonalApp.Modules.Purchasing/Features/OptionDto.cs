namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features;

/// <summary>An entry in a drop-down list: the id of a row and the text to show for it.</summary>
internal sealed record OptionDto(Guid Id, string Name)
{
    /// <summary>The entry that stands for "nothing chosen" in a list of an optional reference.</summary>
    public static OptionDto None(string label) => new(Guid.Empty, label);
}

/// <summary>An entry in a drop-down list of fixed codes.</summary>
internal sealed record ChoiceOption(string Code, string Name);
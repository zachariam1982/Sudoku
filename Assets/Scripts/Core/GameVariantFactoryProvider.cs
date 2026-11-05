using System;

/// <summary>Selects the abstract factory that creates a variant's service family.</summary>
public static class GameVariantFactoryProvider
{
    public static IGameVariantFactory Create(
        GameVariant variant,
        SqliteDatabaseConnection database)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));

        switch (variant)
        {
            case GameVariant.Classic:
                return new ClassicGameVariantFactory(database);
            case GameVariant.Diagonal:
                return new DiagonalGameVariantFactory(database);
            default:
                throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unsupported game variant.");
        }
    }
}

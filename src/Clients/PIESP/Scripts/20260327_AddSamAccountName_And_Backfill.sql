/*
    Cel:
    - dodać kolumnę piesp.Users.SamAccountName, jeśli jeszcze nie istnieje
    - uzupełnić SamAccountName dla istniejących użytkowników
    - założyć unikalny indeks na SamAccountName

    Jak użyć:
    1. Uzupełnij sekcję @Mappings rzeczywistymi danymi.
    2. Uruchom skrypt najpierw na TEST/UAT.
    3. Zweryfikuj wynik sekcji kontrolnych na końcu.

    Założenie:
    - użytkownicy są identyfikowani lokalnie po piesp.Users.BadgeNumber
    - logowanie do API będzie wykonywane po sAMAccountName z AD
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[piesp].[Users]', N'U') IS NULL
        THROW 51000, 'Tabela [piesp].[Users] nie istnieje.', 1;

    IF COL_LENGTH(N'piesp.Users', N'SamAccountName') IS NULL
    BEGIN
        ALTER TABLE [piesp].[Users]
        ADD [SamAccountName] NVARCHAR(256) NULL;
    END;

    DECLARE @Mappings TABLE
    (
        BadgeNumber NVARCHAR(32) NOT NULL,
        SamAccountName NVARCHAR(256) NOT NULL
    );

    /*
        UZUPEŁNIJ TĘ SEKCJĘ.
        Przykład:

        INSERT INTO @Mappings (BadgeNumber, SamAccountName)
        VALUES
            (N'1111', N'jkowalski'),
            (N'2222', N'tnowak');
    */
    INSERT INTO @Mappings (BadgeNumber, SamAccountName)
    VALUES
        (N'1111', N'jkowalski'),
        (N'2222', N'tnowak');

    IF NOT EXISTS (SELECT 1 FROM @Mappings)
        THROW 51001, 'Brak danych w sekcji @Mappings.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM @Mappings
        GROUP BY BadgeNumber
        HAVING COUNT(*) > 1
    )
        THROW 51002, 'W sekcji @Mappings wykryto zduplikowany BadgeNumber.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM @Mappings
        GROUP BY LOWER(LTRIM(RTRIM(SamAccountName)))
        HAVING COUNT(*) > 1
    )
        THROW 51003, 'W sekcji @Mappings wykryto zduplikowany SamAccountName.', 1;

    IF EXISTS
    (
        SELECT m.BadgeNumber
        FROM @Mappings m
        LEFT JOIN [piesp].[Users] u ON u.[BadgeNumber] = m.[BadgeNumber]
        WHERE u.[Id] IS NULL
    )
        THROW 51004, 'Co najmniej jeden BadgeNumber z @Mappings nie istnieje w [piesp].[Users].', 1;

    UPDATE u
    SET u.[SamAccountName] = LOWER(LTRIM(RTRIM(m.[SamAccountName])))
    FROM [piesp].[Users] u
    INNER JOIN @Mappings m
        ON m.[BadgeNumber] = u.[BadgeNumber];

    IF EXISTS
    (
        SELECT 1
        FROM [piesp].[Users]
        WHERE [SamAccountName] IS NOT NULL
        GROUP BY [SamAccountName]
        HAVING COUNT(*) > 1
    )
        THROW 51005, 'Po aktualizacji wykryto zduplikowany SamAccountName w [piesp].[Users].', 1;

    IF EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_Users_SamAccountName'
          AND object_id = OBJECT_ID(N'[piesp].[Users]')
    )
    BEGIN
        DROP INDEX [IX_Users_SamAccountName] ON [piesp].[Users];
    END;

    CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_SamAccountName]
        ON [piesp].[Users] ([SamAccountName])
        WHERE [SamAccountName] IS NOT NULL;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;

PRINT '=== WYNIK MAPOWANIA ===';
SELECT
    u.[Id],
    u.[UserName],
    u.[BadgeNumber],
    u.[SamAccountName],
    u.[IsActive]
FROM [piesp].[Users] u
ORDER BY u.[BadgeNumber];

PRINT '=== UŻYTKOWNICY BEZ SamAccountName ===';
SELECT
    u.[Id],
    u.[UserName],
    u.[BadgeNumber]
FROM [piesp].[Users] u
WHERE u.[SamAccountName] IS NULL
ORDER BY u.[BadgeNumber];

PRINT '=== KONTROLA DUPLIKATÓW SamAccountName ===';
SELECT
    u.[SamAccountName],
    COUNT(*) AS [Count]
FROM [piesp].[Users] u
WHERE u.[SamAccountName] IS NOT NULL
GROUP BY u.[SamAccountName]
HAVING COUNT(*) > 1;


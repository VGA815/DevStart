using DevStart.Application.PatentRegistry;
using DevStart.Domain.PatentRegistry;
using DevStart.Domain.StartupPatents;
using DevStart.SharedKernel;
using Shouldly;

namespace DevStart.UnitTests.Application.PatentRegistry;

public sealed class RospatentDumpParserTests
{
    private const int CurrentYear = 2026;

    [Fact]
    public void Parse_ShouldReadSemicolonSeparatedDump()
    {
        // Russian exports are semicolon-separated as often as comma-separated, and say so nowhere.
        string csv = string.Join('\n',
        [
            "Регистрационный номер;Название;Правообладатель;ИНН правообладателя;Дата регистрации;Статус",
            "2023612345;Тестовая программа;ООО «Ромашка»;7707083893;15.03.2023;действует",
            "2023612346;Вторая программа;ООО «Василёк»;7736207543;01.04.2023;прекращён досрочно",
        ]);

        Result<PatentRegistryParseResult> result =
            RospatentDumpParser.Parse(csv, IntellectualPropertyKind.ComputerProgram, CurrentYear);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Records.Count.ShouldBe(2);
        result.Value.SkippedRows.ShouldBe(0);

        PatentRegistryRecord first = result.Value.Records[0];
        first.Kind.ShouldBe(IntellectualPropertyKind.ComputerProgram);
        first.NumberNormalized.ShouldBe("2023612345");
        first.Title.ShouldBe("Тестовая программа");
        first.HolderName.ShouldBe("ООО «Ромашка»");
        first.HolderInn.ShouldBe("7707083893");
        first.RegisteredOn.ShouldBe(new DateOnly(2023, 3, 15));
        first.Status.ShouldBe(PatentProtectionStatus.Active);

        // "прекращён досрочно" holds both words; the most specific reading wins.
        result.Value.Records[1].Status.ShouldBe(PatentProtectionStatus.EarlyTerminated);
    }

    [Fact]
    public void Parse_ShouldReadCommaSeparatedDumpWithQuotedCells()
    {
        string csv = string.Join('\n',
        [
            "registration number,title,holder,inn,registration date,status",
            "\"RU 2 731 234 C1\",\"Устройство, повышающее КПД\",\"АО «Мотор»\",7736207543,2019-11-05,\"действует\"",
        ]);

        Result<PatentRegistryParseResult> result =
            RospatentDumpParser.Parse(csv, IntellectualPropertyKind.Invention, CurrentYear);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Records.Count.ShouldBe(1);
        result.Value.Records[0].NumberNormalized.ShouldBe("2731234");
        result.Value.Records[0].Title.ShouldBe("Устройство, повышающее КПД");
        result.Value.Records[0].RegisteredOn.ShouldBe(new DateOnly(2019, 11, 5));
    }

    [Fact]
    public void Parse_ShouldSkipUnusableRowsWithoutFailingTheFile()
    {
        string csv = string.Join('\n',
        [
            "Регистрационный номер;Название;Статус",
            "2023612345;Годная;действует",
            "не номер;Мусор;действует",
            "2731234;Неверная форма для этого реестра;действует",
            "2023612345;Тот же номер второй раз;действует",
        ]);

        Result<PatentRegistryParseResult> result =
            RospatentDumpParser.Parse(csv, IntellectualPropertyKind.ComputerProgram, CurrentYear);

        result.IsSuccess.ShouldBeTrue();

        // One malformed line must not cost the rest of the register its refresh — the count is what
        // makes a mostly-unusable dump visible instead of silently thin.
        result.Value.Records.Count.ShouldBe(1);
        result.Value.SkippedRows.ShouldBe(3);
    }

    [Fact]
    public void Parse_ShouldTakeOnlyValidInnAndIgnoreGarbage()
    {
        string csv = string.Join('\n',
        [
            "Регистрационный номер;ИНН правообладателя;Правообладатель",
            "2023612345;ИНН 7707083893;ООО «Ромашка»",
            "2023612346;0000000000;ООО «Пустышка»",
            "2023612347;7707083893, 7736207543;ООО «Ромашка», АО «Мотор»",
        ]);

        Result<PatentRegistryParseResult> result =
            RospatentDumpParser.Parse(csv, IntellectualPropertyKind.ComputerProgram, CurrentYear);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Records[0].HolderInn.ShouldBe("7707083893");

        // A number that fails its check digit is not stored at all: a wrong ИНН would produce a wrong
        // comparison, and a wrong match is worse than a missing one.
        result.Value.Records[1].HolderInn.ShouldBeNull();

        // Co-owned records keep the first valid ИНН; the full holder text stays beside it, so the
        // reader sees the co-owners even though only one ИНН is comparable.
        result.Value.Records[2].HolderInn.ShouldBe("7707083893");
        result.Value.Records[2].HolderName.ShouldBe("ООО «Ромашка», АО «Мотор»");
    }

    [Theory]
    [InlineData("действует", PatentProtectionStatus.Active)]
    [InlineData("Действие патента прекращено", PatentProtectionStatus.Terminated)]
    [InlineData("Действие прекращено досрочно", PatentProtectionStatus.EarlyTerminated)]
    [InlineData("срок действия истёк", PatentProtectionStatus.Terminated)]
    [InlineData("что-то новое", PatentProtectionStatus.Unknown)]
    [InlineData("", PatentProtectionStatus.Unknown)]
    public void Parse_ShouldMapStatusText(string statusText, PatentProtectionStatus expected)
    {
        string csv = $"Регистрационный номер;Статус\n2023612345;{statusText}";

        Result<PatentRegistryParseResult> result =
            RospatentDumpParser.Parse(csv, IntellectualPropertyKind.ComputerProgram, CurrentYear);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Records[0].Status.ShouldBe(expected);
    }

    [Fact]
    public void Parse_ShouldRefuseALayoutItCannotRead()
    {
        // A file whose columns cannot be found is not provenance for anything, so it writes nothing —
        // and the error names what was expected against what was found.
        string csv = "колонка;другая колонка\n1;2";

        Result<PatentRegistryParseResult> result =
            RospatentDumpParser.Parse(csv, IntellectualPropertyKind.ComputerProgram, CurrentYear);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("PatentRegistry.UnreadableDataset");
        result.Error.Description.ShouldContain("другая колонка");
    }

    [Fact]
    public void Parse_ShouldFail_WhenNoRowSurvives()
    {
        string csv = "Регистрационный номер;Статус\nне номер;действует";

        Result<PatentRegistryParseResult> result =
            RospatentDumpParser.Parse(csv, IntellectualPropertyKind.ComputerProgram, CurrentYear);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("PatentRegistry.EmptyDataset");
    }

    [Fact]
    public void Parse_ShouldSurviveByteOrderMarkAndPreamble()
    {
        string csv = "\uFEFFВыгрузка реестра программ для ЭВМ\n\n"
            + "Регистрационный номер;Название\n2023612345;Программа";

        Result<PatentRegistryParseResult> result =
            RospatentDumpParser.Parse(csv, IntellectualPropertyKind.ComputerProgram, CurrentYear);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Records.Count.ShouldBe(1);
    }
}

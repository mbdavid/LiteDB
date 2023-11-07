namespace LiteDB.Tests.Internals.Engine;

public class CheckpointAction_Tests
{
    [Fact]
    public void Checkpoint_OverrideDatafilePage_ClearLogPages()
    {

        #region Arrange

        var sut = new CheckpointActions();

        // datapages ends here
        var lastPageID = 20u;

        // adding log pages at position
        var logPages = new List<LogPageHeader>
        {
            new() { PositionID = 17, PageID = 10, TransactionID = 1, IsConfirmed = false },
            new() { PositionID = 18, PageID = 10, TransactionID = 1, IsConfirmed = false },
            new() { PositionID = 19, PageID = 10, TransactionID = 1, IsConfirmed = false },
            new() { PositionID = 20, PageID = 10, TransactionID = 1, IsConfirmed = true }
        };

        // update lastPageID
        lastPageID = Math.Max(lastPageID, logPages.Max(x => x.PageID));

        // get start temp positionID and confirm transactions
        var startTempPositionID = Math.Max(lastPageID, logPages[^1].PositionID) + 1;
        var confirmedTransactions = new HashSet<int>(logPages.Where(x => x.IsConfirmed).Select(x => x.TransactionID));

        // define temp pages
        var tempPages = new List<LogPageHeader>();

        #endregion

        #region Act

        var actions = sut.GetActions(
            logPages,
            confirmedTransactions,
            lastPageID,
            startTempPositionID,
            tempPages).ToArray();

        #endregion

        #region Asserts

        actions.Length.Should().Be(4);

        // action #0
        actions[0].Action.Should().Be(CheckpointActionType.ClearPage);
        actions[0].PositionID.Should().Be(17);
        actions[0].TargetPositionID.Should().Be(0);
        actions[0].MustClear.Should().BeFalse();

        // action #1
        actions[1].Action.Should().Be(CheckpointActionType.ClearPage);
        actions[1].PositionID.Should().Be(18);
        actions[1].TargetPositionID.Should().Be(0);
        actions[1].MustClear.Should().BeFalse();

        // action #2
        actions[2].Action.Should().Be(CheckpointActionType.ClearPage);
        actions[2].PositionID.Should().Be(19);
        actions[2].TargetPositionID.Should().Be(0);
        actions[2].MustClear.Should().BeFalse();

        // action #3
        actions[3].Action.Should().Be(CheckpointActionType.CopyToDataFile);
        actions[3].PositionID.Should().Be(20);
        actions[3].TargetPositionID.Should().Be(10);
        actions[3].MustClear.Should().BeTrue();

        #endregion

    }

    [Fact]
    public void Checkpoint_AfterLastPageID_MustClearFalse()
    {

        #region Arrange

        var sut = new CheckpointActions();

        // datapages ends here
        var lastPageID = 10u;

        // adding log pages at position
        var logPages = new List<LogPageHeader>
        {
            new() { PositionID = 17, PageID = 10, TransactionID = 1, IsConfirmed = true }
        };

        // update lastPageID
        lastPageID = Math.Max(lastPageID, logPages.Max(x => x.PageID));

        // get start temp positionID and confirm transactions
        var startTempPositionID = Math.Max(lastPageID, logPages[^1].PositionID) + 1;
        var confirmedTransactions = new HashSet<int>(logPages.Where(x => x.IsConfirmed).Select(x => x.TransactionID));

        // define temp pages
        var tempPages = new List<LogPageHeader>();

        #endregion

        #region Act

        var actions = sut.GetActions(
            logPages, 
            confirmedTransactions, 
            lastPageID, 
            startTempPositionID, 
            tempPages).ToArray();

        #endregion

        #region Asserts

        actions.Length.Should().Be(1);

        // action #0
        actions[0].Action.Should().Be(CheckpointActionType.CopyToDataFile);
        actions[0].PositionID.Should().Be(17);
        actions[0].TargetPositionID.Should().Be(10);
        actions[0].MustClear.Should().BeFalse();

        #endregion

    }

    [Fact]
    public void Checkpoint_BeforeLastPageID_MustClearTrue()
    {

        #region Arrange

        var sut = new CheckpointActions();

        // datapages ends here
        var lastPageID = 17u;

        // adding log pages at position
        var logPages = new List<LogPageHeader>
        {
            new() { PositionID = 17, PageID = 10, TransactionID = 1, IsConfirmed = true }
        };

        // update lastPageID
        lastPageID = Math.Max(lastPageID, logPages.Max(x => x.PageID));

        // get start temp positionID and confirm transactions
        var startTempPositionID = Math.Max(lastPageID, logPages[^1].PositionID) + 1;
        var confirmedTransactions = new HashSet<int>(logPages.Where(x => x.IsConfirmed).Select(x => x.TransactionID));

        // define temp pages
        var tempPages = new List<LogPageHeader>();

        #endregion

        #region Act

        var actions = sut.GetActions(
            logPages,
            confirmedTransactions,
            lastPageID,
            startTempPositionID,
            tempPages).ToArray();

        #endregion

        #region Asserts

        actions.Length.Should().Be(1);

        // action #0
        actions[0].Action.Should().Be(CheckpointActionType.CopyToDataFile);
        actions[0].PositionID.Should().Be(17);
        actions[0].TargetPositionID.Should().Be(10);
        actions[0].MustClear.Should().BeTrue();

        #endregion

    }

    [Theory]
    [InlineData(true, 0, 0)]
    [InlineData(true, 19, 20)]
    [InlineData(true, 20, 20)]
    [InlineData(false, 21, 20)]
    public void Checkpoint_TransactionNotConfirmed_ClearPages(bool shouldClear, uint positionID, uint lastPageId)
    {

        #region Arrange

        var sut = new CheckpointActions();

        // datapages ends here
        var lastPageID = lastPageId;

        // adding log pages at position
        var logPages = new List<LogPageHeader>
        {
            new() { PositionID = positionID, PageID = 13, TransactionID = 1, IsConfirmed = false }
        };

        // update lastPageID
        lastPageID = Math.Max(lastPageID, logPages.Max(x => x.PageID));

        // get start temp positionID and confirm transactions
        var startTempPositionID = Math.Max(lastPageID, logPages[^1].PositionID) + 1;
        var confirmedTransactions = new HashSet<int>(logPages.Where(x => x.IsConfirmed).Select(x => x.TransactionID));

        // define temp pages
        var tempPages = new List<LogPageHeader>();

        #endregion

        #region Act

        var actions = sut.GetActions(
            logPages,
            confirmedTransactions,
            lastPageID,
            startTempPositionID,
            tempPages).ToArray();

        #endregion

        #region Asserts

        if(shouldClear)
        {
            actions.Length.Should().Be(1);
            // action #0
            actions[0].Action.Should().Be(CheckpointActionType.ClearPage);
            actions[0].PositionID.Should().Be(positionID);
            actions[0].TargetPositionID.Should().Be(0);
            actions[0].MustClear.Should().BeFalse();
        }
        else
        {
            actions.Length.Should().Be(0);
        }

        #endregion

    }

    [Fact]
    public void Checkpoint_OverrideLogPage_ShouldCopyToTempfile()
    {

        #region Arrange

        var sut = new CheckpointActions();

        // datapages ends here
        var lastPageID = 20u;

        // adding log pages at position
        var logPages = new List<LogPageHeader>
        {
            new() { PositionID = 17, PageID = 18, TransactionID = 1, IsConfirmed = false },
            new() { PositionID = 18, PageID = 11, TransactionID = 1, IsConfirmed = true }
        };

        // update lastPageID
        lastPageID = Math.Max(lastPageID, logPages.Max(x => x.PageID));

        // get start temp positionID and confirm transactions
        var startTempPositionID = Math.Max(lastPageID, logPages[^1].PositionID) + 1;
        var confirmedTransactions = new HashSet<int>(logPages.Where(x => x.IsConfirmed).Select(x => x.TransactionID));

        // define temp pages
        var tempPages = new List<LogPageHeader>();

        #endregion

        #region Act

        var actions = sut.GetActions(
            logPages,
            confirmedTransactions,
            lastPageID,
            startTempPositionID,
            tempPages).ToArray();

        #endregion

        #region Asserts

        actions.Length.Should().Be(3);

        // action #0
        actions[0].Action.Should().Be(CheckpointActionType.CopyToTempFile);
        actions[0].PositionID.Should().Be(18);
        actions[0].TargetPositionID.Should().Be(21);
        actions[0].MustClear.Should().BeFalse();

        // action #1
        actions[1].Action.Should().Be(CheckpointActionType.CopyToDataFile);
        actions[1].PositionID.Should().Be(17);
        actions[1].TargetPositionID.Should().Be(18);
        actions[1].MustClear.Should().BeTrue();

        // action #2
        actions[2].Action.Should().Be(CheckpointActionType.CopyToDataFile);
        actions[2].PositionID.Should().Be(21);
        actions[2].TargetPositionID.Should().Be(11);
        actions[2].MustClear.Should().BeFalse();

        #endregion

    }

    [Fact]
    public void Checkpoint_PreexistingTempInMiddleLog_ShouldDirectlyCopyToDatafile()
    {

        #region Arrange

        var sut = new CheckpointActions();

        // datapages ends here
        var lastPageID = 20u;

        // adding log pages at position
        var logPages = new List<LogPageHeader>
        {
            new() { PositionID = 17, PageID = 10, TransactionID = 1, IsConfirmed = false },
            new() { PositionID = 19, PageID = 12, TransactionID = 1, IsConfirmed = true }
        };

        // update lastPageID
        lastPageID = Math.Max(lastPageID, logPages.Max(x => x.PageID));

        // get start temp positionID and confirm transactions
        var startTempPositionID = Math.Max(lastPageID, logPages[^1].PositionID) + 1;
        var confirmedTransactions = new HashSet<int>(logPages.Where(x => x.IsConfirmed).Select(x => x.TransactionID));

        // define temp pages
        var tempPages = new List<LogPageHeader>
        {
            new() { PositionID = 18, PageID = 11, TransactionID = 1, IsConfirmed = false }
        };

        #endregion

        #region Act

        var actions = sut.GetActions(
            logPages,
            confirmedTransactions,
            lastPageID,
            startTempPositionID,
            tempPages).ToArray();

        #endregion

        #region Asserts

        actions.Length.Should().Be(3);

        // action #0
        actions[0].Action.Should().Be(CheckpointActionType.CopyToDataFile);
        actions[0].PositionID.Should().Be(17);
        actions[0].TargetPositionID.Should().Be(10);
        actions[0].MustClear.Should().BeTrue();

        // action #1
        actions[1].Action.Should().Be(CheckpointActionType.CopyToDataFile);
        actions[1].PositionID.Should().Be(21);
        actions[1].TargetPositionID.Should().Be(11);
        actions[1].MustClear.Should().BeFalse();

        // action #2
        actions[2].Action.Should().Be(CheckpointActionType.CopyToDataFile);
        actions[2].PositionID.Should().Be(19);
        actions[2].TargetPositionID.Should().Be(12);
        actions[2].MustClear.Should().BeTrue();

        #endregion

    }

    [Fact]
    public void Checkpoint_PreexistingTempBeforeLog_ShouldDirectlyCopyToDatafile()
    {

        #region Arrange

        var sut = new CheckpointActions();

        // datapages ends here
        var lastPageID = 20u;

        // adding log pages at position
        var logPages = new List<LogPageHeader>
        {
            new() { PositionID = 18, PageID = 11, TransactionID = 1, IsConfirmed = false },
            new() { PositionID = 19, PageID = 12, TransactionID = 1, IsConfirmed = true }
        };

        // update lastPageID
        lastPageID = Math.Max(lastPageID, logPages.Max(x => x.PageID));

        // get start temp positionID and confirm transactions
        var startTempPositionID = Math.Max(lastPageID, logPages[^1].PositionID) + 1;
        var confirmedTransactions = new HashSet<int>(logPages.Where(x => x.IsConfirmed).Select(x => x.TransactionID));

        // define temp pages
        var tempPages = new List<LogPageHeader>
        {
            new() { PositionID = 17, PageID = 10, TransactionID = 1, IsConfirmed = false }
        };

        #endregion

        #region Act

        var actions = sut.GetActions(
            logPages,
            confirmedTransactions,
            lastPageID,
            startTempPositionID,
            tempPages).ToArray();

        #endregion

        #region Asserts

        actions.Length.Should().Be(3);

        // action #0
        actions[0].Action.Should().Be(CheckpointActionType.CopyToDataFile);
        actions[0].PositionID.Should().Be(21);
        actions[0].TargetPositionID.Should().Be(10);
        actions[0].MustClear.Should().BeFalse();

        // action #1
        actions[1].Action.Should().Be(CheckpointActionType.CopyToDataFile);
        actions[1].PositionID.Should().Be(18);
        actions[1].TargetPositionID.Should().Be(11);
        actions[1].MustClear.Should().BeTrue();

        // action #2
        actions[2].Action.Should().Be(CheckpointActionType.CopyToDataFile);
        actions[2].PositionID.Should().Be(19);
        actions[2].TargetPositionID.Should().Be(12);
        actions[2].MustClear.Should().BeTrue();

        #endregion

    }

    [Fact]
    public void Checkpoint_PreexistingTempAfterLog_ShouldDirectlyCopyToDatafile()
    {

        #region Arrange

        var sut = new CheckpointActions();

        // datapages ends here
        var lastPageID = 20u;

        // adding log pages at position
        var logPages = new List<LogPageHeader>
        {
            new() { PositionID = 17, PageID = 10, TransactionID = 1, IsConfirmed = false },
            new() { PositionID = 18, PageID = 11, TransactionID = 1, IsConfirmed = true }
        };

        // update lastPageID
        lastPageID = Math.Max(lastPageID, logPages.Max(x => x.PageID));

        // get start temp positionID and confirm transactions
        var startTempPositionID = Math.Max(lastPageID, logPages[^1].PositionID) + 1;
        var confirmedTransactions = new HashSet<int>(logPages.Where(x => x.IsConfirmed).Select(x => x.TransactionID));

        // define temp pages
        var tempPages = new List<LogPageHeader>
        {
            new() { PositionID = 19, PageID = 12, TransactionID = 1, IsConfirmed = false }
        };

        #endregion

        #region Act

        var actions = sut.GetActions(
            logPages,
            confirmedTransactions,
            lastPageID,
            startTempPositionID,
            tempPages).ToArray();

        #endregion

        #region Asserts

        actions.Length.Should().Be(3);

        // action #0
        actions[0].Action.Should().Be(CheckpointActionType.CopyToDataFile);
        actions[0].PositionID.Should().Be(17);
        actions[0].TargetPositionID.Should().Be(10);
        actions[0].MustClear.Should().BeTrue();

        // action #1
        actions[1].Action.Should().Be(CheckpointActionType.CopyToDataFile);
        actions[1].PositionID.Should().Be(18);
        actions[1].TargetPositionID.Should().Be(11);
        actions[1].MustClear.Should().BeTrue();

        // action #2
        actions[2].Action.Should().Be(CheckpointActionType.CopyToDataFile);
        actions[2].PositionID.Should().Be(21);
        actions[2].TargetPositionID.Should().Be(12);
        actions[2].MustClear.Should().BeFalse();

        #endregion

    }

    [Fact]
    public void Checkpoint_DuplicatePage_ShouldUseTempPage()
    {

        #region Arrange

        var sut = new CheckpointActions();

        // datapages ends here
        var lastPageID = 20u;

        // adding log pages at position
        var logPages = new List<LogPageHeader>
        {
            new() { PositionID = 18, PageID = 11, TransactionID = 1, IsConfirmed = false },
            new() { PositionID = 19, PageID = 12, TransactionID = 1, IsConfirmed = false },
            new() { PositionID = 20, PageID = 13, TransactionID = 1, IsConfirmed = true }
        };

        // update lastPageID
        lastPageID = Math.Max(lastPageID, logPages.Max(x => x.PageID));

        // get start temp positionID and confirm transactions
        var startTempPositionID = Math.Max(lastPageID, logPages[^1].PositionID) + 1;
        var confirmedTransactions = new HashSet<int>(logPages.Where(x => x.IsConfirmed).Select(x => x.TransactionID));

        // define temp pages
        var tempPages = new List<LogPageHeader>
        {
            new() { PositionID = 19, PageID = 12, TransactionID = 1, IsConfirmed = false }
        };

        #endregion

        #region Act

        var actions = sut.GetActions(
            logPages,
            confirmedTransactions,
            lastPageID,
            startTempPositionID,
            tempPages).ToArray();

        #endregion

        #region Asserts

        actions.Length.Should().Be(3);

        // action #0
        actions[0].Action.Should().Be(CheckpointActionType.CopyToDataFile);
        actions[0].PositionID.Should().Be(18);
        actions[0].TargetPositionID.Should().Be(11);
        actions[0].MustClear.Should().BeTrue();

        // action #1
        actions[1].Action.Should().Be(CheckpointActionType.CopyToDataFile);
        actions[1].PositionID.Should().Be(21);
        actions[1].TargetPositionID.Should().Be(12);
        actions[1].MustClear.Should().BeFalse();

        // action #2
        actions[2].Action.Should().Be(CheckpointActionType.CopyToDataFile);
        actions[2].PositionID.Should().Be(20);
        actions[2].TargetPositionID.Should().Be(13);
        actions[2].MustClear.Should().BeTrue();

        #endregion

    }
}
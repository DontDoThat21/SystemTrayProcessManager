using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for the ProcessGroup model.
    /// </summary>
    public class ProcessGroupTests
    {
        #region Constructor and Default Values

        [Fact]
        public void Constructor_ShouldSetDefaultId()
        {
            var group = new ProcessGroup();
            Assert.NotEqual(Guid.Empty, group.Id);
        }

        [Fact]
        public void Constructor_ShouldSetEmptyName()
        {
            var group = new ProcessGroup();
            Assert.Equal(string.Empty, group.Name);
        }

        [Fact]
        public void Constructor_ShouldSetEmptyPatterns()
        {
            var group = new ProcessGroup();
            Assert.NotNull(group.ProcessPatterns);
            Assert.Empty(group.ProcessPatterns);
        }

        #endregion

        #region HasPatterns Property

        [Fact]
        public void HasPatterns_EmptyList_ShouldReturnFalse()
        {
            var group = new ProcessGroup { ProcessPatterns = [] };
            
            Assert.False(group.HasPatterns);
        }

        [Fact]
        public void HasPatterns_WithPatterns_ShouldReturnTrue()
        {
            var group = new ProcessGroup 
            { 
                ProcessPatterns = ["chrome*"] 
            };
            
            Assert.True(group.HasPatterns);
        }

        #endregion

        #region IsValid Property

        [Fact]
        public void IsValid_WithNameAndPatterns_ShouldReturnTrue()
        {
            var group = new ProcessGroup
            {
                Name = "Browsers",
                ProcessPatterns = ["chrome*", "firefox*"]
            };
            
            Assert.True(group.IsValid);
        }

        [Fact]
        public void IsValid_EmptyName_ShouldReturnFalse()
        {
            var group = new ProcessGroup
            {
                Name = "",
                ProcessPatterns = ["chrome*"]
            };
            
            Assert.False(group.IsValid);
        }

        [Fact]
        public void IsValid_EmptyPatterns_ShouldReturnFalse()
        {
            var group = new ProcessGroup
            {
                Name = "Browsers",
                ProcessPatterns = []
            };
            
            Assert.False(group.IsValid);
        }

        #endregion

        #region PatternsDisplay Property

        [Fact]
        public void PatternsDisplay_NoPatterns_ShouldReturnNoPatterns()
        {
            var group = new ProcessGroup { ProcessPatterns = [] };
            
            Assert.Contains("no patterns", group.PatternsDisplay);
        }

        [Fact]
        public void PatternsDisplay_SinglePattern_ShouldReturnPattern()
        {
            var group = new ProcessGroup 
            { 
                ProcessPatterns = ["chrome*"] 
            };
            
            Assert.Contains("chrome*", group.PatternsDisplay);
        }

        [Fact]
        public void PatternsDisplay_MultiplePatterns_ShouldJoinWithComma()
        {
            var group = new ProcessGroup 
            { 
                ProcessPatterns = ["chrome*", "firefox*"] 
            };
            
            Assert.Contains("chrome*, firefox*", group.PatternsDisplay);
        }

        [Fact]
        public void PatternsDisplay_MoreThanThreePatterns_ShouldShowPlusMore()
        {
            var group = new ProcessGroup 
            { 
                ProcessPatterns = ["a", "b", "c", "d"] 
            };
            
            Assert.Contains("+1 more", group.PatternsDisplay);
        }

        #endregion

        #region MatchesProcess Method

        [Fact]
        public void MatchesProcess_NullProcessName_ShouldReturnFalse()
        {
            var group = new ProcessGroup 
            { 
                ProcessPatterns = ["*"] 
            };
            
            Assert.False(group.MatchesProcess(null!));
        }

        [Fact]
        public void MatchesProcess_EmptyProcessName_ShouldReturnFalse()
        {
            var group = new ProcessGroup 
            { 
                ProcessPatterns = ["*"] 
            };
            
            Assert.False(group.MatchesProcess(""));
        }

        [Fact]
        public void MatchesProcess_ExactMatch_ShouldReturnTrue()
        {
            var group = new ProcessGroup 
            { 
                ProcessPatterns = ["notepad"] 
            };
            
            Assert.True(group.MatchesProcess("notepad"));
        }

        [Fact]
        public void MatchesProcess_ExactMatchCaseInsensitive_ShouldReturnTrue()
        {
            var group = new ProcessGroup 
            { 
                ProcessPatterns = ["notepad"] 
            };
            
            Assert.True(group.MatchesProcess("NOTEPAD"));
            Assert.True(group.MatchesProcess("Notepad"));
        }

        [Fact]
        public void MatchesProcess_WildcardSuffix_ShouldMatch()
        {
            var group = new ProcessGroup 
            { 
                ProcessPatterns = ["chrome*"] 
            };
            
            Assert.True(group.MatchesProcess("chrome"));
            Assert.True(group.MatchesProcess("chromedriver"));
        }

        [Fact]
        public void MatchesProcess_WildcardPrefix_ShouldMatch()
        {
            var group = new ProcessGroup 
            { 
                ProcessPatterns = ["*helper"] 
            };
            
            Assert.True(group.MatchesProcess("chromehelper"));
            Assert.True(group.MatchesProcess("helper"));
        }

        [Fact]
        public void MatchesProcess_WildcardBoth_ShouldMatch()
        {
            var group = new ProcessGroup 
            { 
                ProcessPatterns = ["*chrome*"] 
            };
            
            Assert.True(group.MatchesProcess("google chrome"));
            Assert.True(group.MatchesProcess("chrome"));
            Assert.True(group.MatchesProcess("chromedriver"));
        }

        [Fact]
        public void MatchesProcess_QuestionMarkWildcard_ShouldMatchSingleChar()
        {
            var group = new ProcessGroup 
            { 
                ProcessPatterns = ["app?"] 
            };
            
            Assert.True(group.MatchesProcess("app1"));
            Assert.True(group.MatchesProcess("appX"));
            Assert.False(group.MatchesProcess("app"));
            Assert.False(group.MatchesProcess("app12"));
        }

        [Fact]
        public void MatchesProcess_NoMatchingPattern_ShouldReturnFalse()
        {
            var group = new ProcessGroup 
            { 
                ProcessPatterns = ["chrome*", "firefox*"] 
            };
            
            Assert.False(group.MatchesProcess("notepad"));
        }

        [Fact]
        public void MatchesProcess_MultiplePatterns_ShouldMatchAny()
        {
            var group = new ProcessGroup 
            { 
                ProcessPatterns = ["chrome*", "firefox*", "edge*"] 
            };
            
            Assert.True(group.MatchesProcess("chrome"));
            Assert.True(group.MatchesProcess("firefox"));
            Assert.True(group.MatchesProcess("msedge"));
            Assert.False(group.MatchesProcess("safari"));
        }

        #endregion

        #region WithAddedPattern Method

        [Fact]
        public void WithAddedPattern_ValidPattern_ShouldAddPattern()
        {
            var group = new ProcessGroup 
            { 
                ProcessPatterns = ["chrome*"] 
            };
            
            var updated = group.WithAddedPattern("firefox*");
            
            Assert.Equal(2, updated.ProcessPatterns.Count);
            Assert.Contains("firefox*", updated.ProcessPatterns);
        }

        [Fact]
        public void WithAddedPattern_EmptyPattern_ShouldNotAdd()
        {
            var group = new ProcessGroup 
            { 
                ProcessPatterns = ["chrome*"] 
            };
            
            var updated = group.WithAddedPattern("");
            
            Assert.Single(updated.ProcessPatterns);
        }

        [Fact]
        public void WithAddedPattern_DuplicatePattern_ShouldNotAdd()
        {
            var group = new ProcessGroup 
            { 
                ProcessPatterns = ["chrome*"] 
            };
            
            var updated = group.WithAddedPattern("chrome*");
            
            Assert.Single(updated.ProcessPatterns);
        }

        [Fact]
        public void WithAddedPattern_DuplicateCaseInsensitive_ShouldNotAdd()
        {
            var group = new ProcessGroup 
            { 
                ProcessPatterns = ["chrome*"] 
            };
            
            var updated = group.WithAddedPattern("CHROME*");
            
            Assert.Single(updated.ProcessPatterns);
        }

        [Fact]
        public void WithAddedPattern_ShouldUpdateLastModified()
        {
            var oldTimestamp = DateTime.UtcNow.AddDays(-1);
            var group = new ProcessGroup 
            { 
                ProcessPatterns = ["chrome*"],
                LastModified = oldTimestamp
            };
            
            var updated = group.WithAddedPattern("firefox*");
            
            Assert.True(updated.LastModified > oldTimestamp);
        }

        #endregion

        #region WithRemovedPattern Method

        [Fact]
        public void WithRemovedPattern_ExistingPattern_ShouldRemove()
        {
            var group = new ProcessGroup 
            { 
                ProcessPatterns = ["chrome*", "firefox*"] 
            };
            
            var updated = group.WithRemovedPattern("chrome*");
            
            Assert.Single(updated.ProcessPatterns);
            Assert.DoesNotContain("chrome*", updated.ProcessPatterns);
        }

        [Fact]
        public void WithRemovedPattern_CaseInsensitive_ShouldRemove()
        {
            var group = new ProcessGroup 
            { 
                ProcessPatterns = ["chrome*"] 
            };
            
            var updated = group.WithRemovedPattern("CHROME*");
            
            Assert.Empty(updated.ProcessPatterns);
        }

        [Fact]
        public void WithRemovedPattern_NonExistentPattern_ShouldNotChange()
        {
            var group = new ProcessGroup 
            { 
                ProcessPatterns = ["chrome*"] 
            };
            
            var updated = group.WithRemovedPattern("firefox*");
            
            Assert.Single(updated.ProcessPatterns);
        }

        #endregion

        #region Equality

        [Fact]
        public void Equals_SameId_ShouldReturnTrue()
        {
            var id = Guid.NewGuid();
            var group1 = new ProcessGroup { Id = id };
            var group2 = new ProcessGroup { Id = id };
            
            Assert.Equal(group1, group2);
        }

        [Fact]
        public void Equals_DifferentId_ShouldReturnFalse()
        {
            var group1 = new ProcessGroup();
            var group2 = new ProcessGroup();
            
            Assert.NotEqual(group1, group2);
        }

        [Fact]
        public void GetHashCode_SameId_ShouldBeSame()
        {
            var id = Guid.NewGuid();
            var group1 = new ProcessGroup { Id = id };
            var group2 = new ProcessGroup { Id = id };
            
            Assert.Equal(group1.GetHashCode(), group2.GetHashCode());
        }

        #endregion

        #region ToString

        [Fact]
        public void ToString_ShouldContainName()
        {
            var group = new ProcessGroup { Name = "TestGroup" };
            
            var result = group.ToString();
            
            Assert.Contains("TestGroup", result);
        }

        [Fact]
        public void ToString_ShouldContainPatterns()
        {
            var group = new ProcessGroup 
            { 
                ProcessPatterns = ["chrome*", "firefox*"] 
            };
            
            var result = group.ToString();
            
            Assert.Contains("chrome*", result);
            Assert.Contains("firefox*", result);
        }

        #endregion
    }
}

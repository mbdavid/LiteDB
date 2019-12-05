using System.Linq;
using FluentAssertions;
using LiteDB.Engine;
using Xunit;

namespace LiteDB.Internals
{
    public class FreeSlots_Tests
    {
        /// <summary>
        /// FreeBytes ranges on page slot for free list page
        /// 90% - 100% = 0 (7344 - 8160)
        /// 75% -  90% = 1 (6120 - 7343)
        /// 60% -  75% = 2 (4896 - 6119)
        /// 30% -  60% = 3 (2448 - 4895)
        ///  0% -  30% = 4 (0000 - 2447)
        /// </summary>
        [Fact]
        public void FreeIndexSlot_Ranges()
        {
            BasePage.FreeIndexSlot(0).Should().Be(4);
            BasePage.FreeIndexSlot(200).Should().Be(4);
            BasePage.FreeIndexSlot(2447).Should().Be(4);

            BasePage.FreeIndexSlot(2448).Should().Be(3);
            BasePage.FreeIndexSlot(4895).Should().Be(3);

            BasePage.FreeIndexSlot(4896).Should().Be(2);
            BasePage.FreeIndexSlot(6119).Should().Be(2);

            BasePage.FreeIndexSlot(6120).Should().Be(1);
            BasePage.FreeIndexSlot(7343).Should().Be(1);

            BasePage.FreeIndexSlot(7344).Should().Be(0);
            BasePage.FreeIndexSlot(8160).Should().Be(0);
        }

        [Fact]
        public void MinimumIndexSlot_Ranges()
        {
            BasePage.GetMinimumIndexSlot(1).Should().Be(3);
            BasePage.GetMinimumIndexSlot(200).Should().Be(3);
            BasePage.GetMinimumIndexSlot(2447).Should().Be(3);

            BasePage.GetMinimumIndexSlot(2448).Should().Be(2);
            BasePage.GetMinimumIndexSlot(4895).Should().Be(2);

            BasePage.GetMinimumIndexSlot(4896).Should().Be(1);
            BasePage.GetMinimumIndexSlot(6119).Should().Be(1);

            BasePage.GetMinimumIndexSlot(6120).Should().Be(0);
            BasePage.GetMinimumIndexSlot(7343).Should().Be(0);

            // need new page (returns -1)
            BasePage.GetMinimumIndexSlot(8160).Should().Be(-1);
            BasePage.GetMinimumIndexSlot(7344).Should().Be(-1);

        }
    }
}
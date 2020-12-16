using System;
using Aicup2020.Model;
using Aicup2020.MyModel;
using Xunit;

namespace XUnitTest
{
    public class Vec2IntTests
    {
        [Fact]
        public void RadiusTest()
        {
            Vec2Int p = new Vec2Int(1, 1);
            var radius = p.Radius(1);

            Assert.True(radius.Count == 4);
        }
    }
}

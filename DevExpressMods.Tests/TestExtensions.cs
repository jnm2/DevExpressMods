using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DevExpressMods.Tests
{
    [TestFixture]
    public class TestExtensions
    {
        private sealed class RecursiveStructure
        {
            public RecursiveStructure(string tag, ICollection<RecursiveStructure> childStructures)
            {
                Tag = tag;
                ChildStructures = childStructures;
            }

            public string Tag { get; }
            public ICollection<RecursiveStructure> ChildStructures { get; }
        }

        [Test]
        public void TestSelectManyRecursive()
        {
            var recursiveStructure = new RecursiveStructure("1", new[]
            {
                new RecursiveStructure("1.1", new RecursiveStructure[0]),
                new RecursiveStructure("1.2", new[]
                {
                    new RecursiveStructure("1.2.1", new[] { new RecursiveStructure("1.2.1.1", null) }),
                    new RecursiveStructure("1.2.2", new[] { new RecursiveStructure("1.2.2.1", null) })
                }),

                new RecursiveStructure("1.3", null),
            });

            Assert.That(new[] {recursiveStructure}.SelectManyRecursive(_ => _.ChildStructures).Select(_ => _.Tag), Is.EqualTo(
                new[]
                {
                    "1",
                    "1.1",
                    "1.2",
                    "1.2.1",
                    "1.2.1.1",
                    "1.2.2",
                    "1.2.2.1",
                    "1.3"
                }));
        }
    }
}

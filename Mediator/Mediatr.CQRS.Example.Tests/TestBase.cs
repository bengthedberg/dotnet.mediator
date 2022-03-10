using NUnit.Framework;
using System.Threading.Tasks;

namespace Mediatr.CQRS.Example.Tests;

using static Testing;

public class TestBase
{

    [SetUp]
    public void TestSetUp()
    {
         ResetState();
    }
}

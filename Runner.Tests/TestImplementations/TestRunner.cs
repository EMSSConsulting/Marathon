using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marathon.Tests.TestImplementations
{
    class TestRunner : Marathon.Runner
    {
        public TestRunner(string[] args)
            : base(args)
        {

        }

        protected override void Initialize()
        {
            base.Initialize();
            Network = new TestNetwork(this);
        }
    }
}

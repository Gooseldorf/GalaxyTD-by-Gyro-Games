using System.Collections.Generic;

namespace TestingAgent
{
    public sealed class DirectivesTestResult : TestResult
    {
        public List<WeaponPart> TestedDirectives;
        public List<string> directivesSequence;
    }
}
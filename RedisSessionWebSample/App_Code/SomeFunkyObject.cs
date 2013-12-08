using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace RedisSessionWebSample
{
    [Serializable]
    internal class SubComplexObject
    {
        public int SomeID { get; set; }
        public long SomeLongValue { get; set; }
        public string SomeChunkyString01 { get; set; }
        public string SomeChunkyString02 { get; set; }
        public string SomeChunkyString03 { get; set; }

        public override string ToString()
        {
            StringBuilder sbOut = new StringBuilder(1024);
            const string bLine = "<li>";
            const string eLine = "</li>";
            sbOut.Append("<ul>");
            sbOut.Append(bLine).Append("SomeID:").Append(this.SomeID.ToString()).Append(eLine);
            sbOut.Append(bLine).Append("SomeLongValue:").AppendLine(this.SomeLongValue.ToString()).Append(eLine);
            sbOut.Append(bLine).Append("SomeChunkyString01:").AppendLine(this.SomeChunkyString01.ToString()).Append(eLine);
            sbOut.Append(bLine).Append("SomeChunkyString02:").AppendLine(this.SomeChunkyString02.ToString()).Append(eLine);
            sbOut.Append(bLine).Append("SomeChunkyString03:").AppendLine(this.SomeChunkyString03.ToString()).Append(eLine);
            sbOut.AppendLine("</ul>");

            return sbOut.ToString();
        }
    }

    [Serializable]
    internal class ComplexObject
    {
        public int ID { get; set; }
        public long LongValue { get; set; }
        public string ChunkyString01 { get; set; }
        public string ChunkyString02 { get; set; }
        public List<SubComplexObject> Subs { get; set; }

        public override string ToString()
        {
            StringBuilder sbOut = new StringBuilder(1024);
            const string endPara = "</p>";
            sbOut.Append("<p>ID:").Append(this.ID.ToString()).Append(endPara);
            sbOut.Append("<p>LongValue:").Append(this.LongValue.ToString()).Append(endPara);
            sbOut.Append("<p>ChunkyString01:").Append(this.ChunkyString01).Append(endPara);
            sbOut.Append("<p>ChunkyString02:").Append(this.ChunkyString02).AppendLine(endPara);
            sbOut.Append("<div style='padding-left: 5em;'><p>Subs:").Append(this.Subs.Count.ToString()).Append(endPara);
            foreach (SubComplexObject sub in Subs)
                sbOut.Append(sub.ToString());
            sbOut.AppendLine("</div>");
            return sbOut.ToString();
        }
    }
    [Serializable]
    public class SomeFunkyObjectSubObject
    {
        public string sub_GUID { get; set; }
        public double sub_dRand { get; set; }
        public int sub_iRand { get; set; }
        public long sub_lRand { get; set; }
        public decimal sub_mRand { get; set; }

        public SomeFunkyObjectSubObject()
        {
            sub_GUID = (Guid.NewGuid()).ToString("N");
            sub_dRand = DateTime.Now.Millisecond;
            sub_iRand = DateTime.Now.Second;
            sub_mRand = (decimal)(sub_lRand = DateTime.Now.Ticks);
        }
    }

    [Serializable]
    public class SomeFunkyObject
    {
        public string GUID { get; set; }
        public double dRand { get; set; }
        public int iRand { get; set; }
        public long lRand { get; set; }
        public decimal mRand { get; set; }
        public SomeFunkyObjectSubObject childObj { get; set; }

        public SomeFunkyObject()
        {
            GUID = Guid.NewGuid().ToString("N");
            dRand = DateTime.Now.Millisecond;
            iRand = DateTime.Now.Second;
            mRand = (decimal)(lRand = DateTime.Now.Ticks);
            childObj = new SomeFunkyObjectSubObject();
        }
    }

  
}
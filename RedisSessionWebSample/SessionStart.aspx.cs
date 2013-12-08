using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace RedisSessionWebSample
{
    public partial class SessionStart : System.Web.UI.Page
    {

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                PopulateOutput();
        }
        protected void ButCreateNewValues_Click(object sender, EventArgs e)
        {
            CreateSessionObjects();
            ViewState["values"] = HttpUtility.HtmlEncode( GetSessionValues() );
            PopulateOutput();
        }

        protected void ButCompare_Click(object sender, EventArgs e)
        {
            PopulateOutput();
            string prev = ViewState["values"] == null ? null : ViewState["values"].ToString();
            if (string.IsNullOrEmpty(prev))
            {
                litOutput.Text = "<h5>We have nothing recorded.</h5>" + litOutput.Text;
            }
            else
            {
                string current = HttpUtility.HtmlEncode(GetSessionValues());
                litOutput.Text = ((prev != current) ? "<h2>Whoa! Previously recorded values did not compare!</h2>" : "<h3>All Good! Previous values recorded are the same as in Session.</h3>") + litOutput.Text;
            }
        }

        protected void ButClear_Click(object sender, EventArgs e)
        {
            ClearSessionObjects();
            ViewState["values"] = null;
            PopulateOutput();
        }

        protected void ButNuke_Click(object sender, EventArgs e)
        {
            Session.Clear();
            ViewState["values"] = null;
            PopulateOutput();
        }

        protected void PopulateOutput()
        {
            litOutput.Text = GetSessionValues();
        }

        protected string GetSessionValues()
        {
            StringBuilder sbVal = new StringBuilder(2048);
            const string bLine = "<h5>";
            const string mLine = "</h5><div class='inner_div'>";
            const string eLine = "</div>";

            sbVal.Append(bLine).Append(SessionKeys.KeyString1).Append(mLine).Append(Session[SessionKeys.KeyString1] == null ? "(null)" : Session[SessionKeys.KeyString1].ToString()).Append(eLine);
            sbVal.Append(bLine).Append(SessionKeys.KeyString2).Append(mLine).Append(Session[SessionKeys.KeyString2] == null ? "(null)" : Session[SessionKeys.KeyString2].ToString()).Append(eLine);
            sbVal.Append(bLine).Append(SessionKeys.KeyString3).Append(mLine).Append(Session[SessionKeys.KeyString3] == null ? "(null)" : Session[SessionKeys.KeyString3].ToString()).Append(eLine);
            sbVal.Append(bLine).Append(SessionKeys.KeyString4).Append(mLine).Append(Session[SessionKeys.KeyString4] == null ? "(null)" : Session[SessionKeys.KeyString4].ToString()).Append(eLine);

            sbVal.Append(bLine).Append(SessionKeys.KeyInt1).Append(mLine).Append(Session[SessionKeys.KeyInt1] == null ? "(null)" : Session[SessionKeys.KeyInt1].ToString()).Append(eLine);
            sbVal.Append(bLine).Append(SessionKeys.KeyInt2).Append(mLine).Append(Session[SessionKeys.KeyInt2] == null ? "(null)" : Session[SessionKeys.KeyInt2].ToString()).Append(eLine);
            sbVal.Append(bLine).Append(SessionKeys.KeyInt3).Append(mLine).Append(Session[SessionKeys.KeyInt3] == null ? "(null)" : Session[SessionKeys.KeyInt3].ToString()).Append(eLine);
            sbVal.Append(bLine).Append(SessionKeys.KeyInt4).Append(mLine).Append(Session[SessionKeys.KeyInt4] == null ? "(null)" : Session[SessionKeys.KeyInt4].ToString()).Append(eLine);

            sbVal.Append(bLine).Append(SessionKeys.KeyObj1).Append(mLine).Append(Session[SessionKeys.KeyObj1] == null ? "(null)" : Session[SessionKeys.KeyObj1].ToString()).Append(eLine);
            sbVal.Append(bLine).Append(SessionKeys.KeyObj2).Append(mLine).Append(Session[SessionKeys.KeyObj2] == null ? "(null)" : Session[SessionKeys.KeyObj2].ToString()).Append(eLine);
            sbVal.Append(bLine).Append(SessionKeys.KeyObj3).Append(mLine).Append(Session[SessionKeys.KeyObj3] == null ? "(null)" : Session[SessionKeys.KeyObj3].ToString()).Append(eLine);
            sbVal.Append(bLine).Append(SessionKeys.KeyObj4).Append(mLine).Append(Session[SessionKeys.KeyObj4] == null ? "(null)" : Session[SessionKeys.KeyObj4].ToString()).Append(eLine);

            return sbVal.ToString();
        }

        protected void CreateSessionObjects()
        {
            Session[SessionKeys.KeyString1] = "val:" + (DateTime.Now.Second).ToString("00");
            Session[SessionKeys.KeyString2] = "val:" + (DateTime.Now.Second + 5).ToString("00");
            Session[SessionKeys.KeyString3] = "val:" + (DateTime.Now.Second + 10).ToString("00");
            Session[SessionKeys.KeyString4] = "val:" + (DateTime.Now.Second + 20).ToString("00");

            Session[SessionKeys.KeyInt1] = DateTime.Now.Second;
            Session[SessionKeys.KeyInt2] = DateTime.Now.Second + 1;
            Session[SessionKeys.KeyInt3] = DateTime.Now.Second + 5;
            Session[SessionKeys.KeyInt4] = DateTime.Now.Second + 10;

            Session[SessionKeys.KeyObj1] = CreateComplexObj(((int)DateTime.Now.Ticks) % 87);
            Session[SessionKeys.KeyObj2] = CreateComplexObj(((int)DateTime.Now.Ticks) % 89);
            Session[SessionKeys.KeyObj3] = CreateComplexObj(((int)DateTime.Now.Ticks) % 83);
            Session[SessionKeys.KeyObj4] = CreateComplexObj(((int)DateTime.Now.Ticks) % 87);
        }

        protected void ClearSessionObjects()
        {
            Session[SessionKeys.KeyString1] = null;
            Session[SessionKeys.KeyString2] = null;
            Session[SessionKeys.KeyString3] = null;
            Session[SessionKeys.KeyString4] = null;

            Session[SessionKeys.KeyInt1] = null;
            Session[SessionKeys.KeyInt2] = null;
            Session[SessionKeys.KeyInt3] = null;
            Session[SessionKeys.KeyInt4] = null;

            Session[SessionKeys.KeyObj1] = null;
            Session[SessionKeys.KeyObj2] = null;
            Session[SessionKeys.KeyObj3] = null;
            Session[SessionKeys.KeyObj4] = null;
        }

        private ComplexObject CreateComplexObj(int factor)
        {
            ComplexObject ret = new ComplexObject()
            {
                ID = (int)(DateTime.Now.Ticks % 19999),
                LongValue = DateTime.Now.Ticks % 19889,
                ChunkyString01 = GenerateRandomString(factor),
                ChunkyString02 = GenerateRandomString(factor << 1),
                Subs = new List<SubComplexObject>()
            };

            for (int i = factor > 0 ? factor%15 : (-factor)%17; i >= 0; i--)
            {
                ret.Subs.Add(new SubComplexObject()
                {
                    SomeChunkyString01 = GenerateRandomString(i),
                    SomeChunkyString02 = GenerateRandomString(i << 1),
                    SomeChunkyString03 = GenerateRandomString(i << 2),
                    SomeID = (int)DateTime.Now.Ticks,
                    SomeLongValue = DateTime.Now.Ticks
                });
            }

            return ret;
        }

        private string GenerateRandomString(int factor)
        {
            StringBuilder sbVal = new StringBuilder(512);

            Guid gu = Guid.NewGuid();
            while (factor >= 0)
            {
                sbVal.Append(gu.ToString("N"));
                factor--;
            }
            return sbVal.ToString();
        }
    }
}
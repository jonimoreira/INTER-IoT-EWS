using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;

namespace INTERIoTEWS.UIprototype
{
    public partial class Form4 : Form
    {
        public Form4()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                ExecuteSPARQL();
            }
            catch (Exception ex)
            {
                textBox3.Text = ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine + (ex.InnerException != null ? ex.InnerException.Message : "NO InnerException");
            }
        }

        private void ExecuteSPARQL()
        {
            string result = string.Empty;

            string data = textBox1.Text;
            string sparqlQuery = textBox2.Text;
            textBox3.Text = string.Empty;

            var jsonLdParser = new JsonLdParser();
            TripleStore tStore = new TripleStore();
            using (var reader = new System.IO.StringReader(data))
            {
                jsonLdParser.Load(tStore, reader);
            }
            
            Object results = tStore.ExecuteQuery(sparqlQuery);

            bool printHeader = true;

            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                textBox3.Text = "rset.Count: " + rset.Count + Environment.NewLine;

                foreach (SparqlResult spqlResult in rset)
                {
                    string row = string.Empty;
                    string header = string.Empty;
                    foreach (string variable in spqlResult.Variables)
                    {
                        row += spqlResult[variable].ToString() + " || ";
                        header += variable + "    ||    ";
                    }

                    if (printHeader)
                    {
                        textBox3.Text += header + Environment.NewLine;
                        printHeader = false;
                    }

                    textBox3.Text += row + Environment.NewLine + "<br/>" + Environment.NewLine;
                }
            }
            else
            {
                textBox3.Text = "result not a SparqlResultSet";
            }
            
        }

        private void Form4_Load(object sender, EventArgs e)
        {

        }
    }
}

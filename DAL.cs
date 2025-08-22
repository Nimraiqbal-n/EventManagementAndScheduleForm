using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.SqlClient;
using System.Xml;

namespace FACULTY_PORTAL.Models
{
    public class DAL
    {
        //172.16.24.9
        //public SqlConnection conn = new SqlConnection("Server = 43.225.99.62; User Id = erplgu;Password=LGU123@LGU321@; Database = LGU_MISDB;Max Pool Size=30000");

        //public SqlConnection conn = new SqlConnection("Data Source=43.225.99.62;Initial Catalog=LGU_MISDB;Persist Security Info=True;User ID=ERPLGU;Password=DFRSC@ERP@DBserver@62; Max Pool Size=20000");
        //public SqlConnection conn = new SqlConnection("Data Source=119.156.25.14;Initial Catalog=LGU_MISDB;Persist Security Info=True;User ID=ERPLGU;Password=DFRSC@ERP@DBserver@62; Max Pool Size=20000");
        public SqlConnection conn = new SqlConnection("Data Source=149.40.228.72;Initial Catalog=LGU_MISDB;Persist Security Info=True;User ID=invalidelgu;Password=DFRSC@Testing@erp@(4244); Max Pool Size=20000");
        //public SqlConnection conn = new SqlConnection("Data Source=119.156.25.12;Initial Catalog=LGU_MISDB;Persist Security Info=True;User ID=invalidelgu;Password=DFRSC@123@erp@(058); Max Pool Size=20000");

        public SqlTransaction objTrans = null;
        public SqlCommand comm = new SqlCommand();
        public SqlDataAdapter da;
        public DataSet ds = new DataSet();
        DataSet ds1 = new DataSet();
        DataSet ds2 = new DataSet();
        DataSet ds3 = new DataSet();
        public DataSet Select(string s)
        {
            ds.Clear();
            ds.Tables.Clear();
            da = new SqlDataAdapter(s, conn);
            da.SelectCommand.Transaction = objTrans;
            da.SelectCommand.CommandTimeout = 1520;
            da.Fill(ds);
            return ds;
        }

        public DataSet SelectSecure(string query, SqlParameter[] parameters)
        {
            conn.Open();
            var cmd = new SqlCommand(query, conn);
            cmd.CommandTimeout = 320;
            if (parameters != null)
            {
                cmd.Parameters.AddRange(parameters);
            }
            var da = new SqlDataAdapter(cmd);
            var ds = new DataSet();   ////  dataset
            da.Fill(ds);
            conn.Close();
            return ds;
        }
        public int ExecuteSecure(string query, SqlParameter[] parameters)
        {
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);

                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();
                conn.Close();
                return rowsAffected;
            }
        }

        //public DS_Att_view Select_DS(string s)
        //{
        //    DS_Att_view ds = new DS_Att_view();
        //    da = new SqlDataAdapter(s, conn);
        //    da.Fill(ds);
        //    return ds;
        //}
        public void conn_open()
        {
            conn.Open();
        }
        public void conn_close()
        {
            conn.Close();
        }
        public void savepoint()
        {
            objTrans = conn.BeginTransaction();
        }
        public void Commit()
        {
            // Throws exception due to foreign key constraint   
            objTrans.Commit();
        }
        public void Rollback()
        {
            objTrans.Rollback();
        }
        public void command(string s)
        {
            comm.Connection = conn;
            comm.CommandText = s;
            comm.ExecuteNonQuery();
        }



        public void command(string s, SqlTransaction transaction = null)
        {
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = s;
                cmd.Transaction = transaction;
                cmd.ExecuteNonQuery();
            }
        }

        public string command2(string s)
        {
            comm.Connection = conn;
            comm.CommandText = s;
            string modified = comm.ExecuteScalar().ToString();
            return modified;
        }

        public XmlReader command_XML(string s)
        {
            XmlReader xr;
            comm.Connection = conn;
            comm.CommandText = s;
            xr = comm.ExecuteXmlReader();
            xr.Read();
            return xr;
        }
        public string[,] Array_Store(DataSet DS)
        {
            string[,] final;
            int r = DS.Tables[0].Rows.Count;
            int c = DS.Tables[0].Columns.Count;
            final = new string[r, c];
            for (int i = 0; i < r; i++)
            {
                for (int j = 0; j < c; j++)
                {
                    final[i, j] = ds.Tables[0].Rows[i][j].ToString();
                }
            }
            return final;
        }
    }
}

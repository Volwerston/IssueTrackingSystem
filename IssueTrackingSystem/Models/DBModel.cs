using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Configuration;
using System.Web.Security;

namespace BTS.Models
{
    public class DBModel
    {
        private SqlConnection connection;

        public bool Open(string connString = "BugTrackingSystem")
        {
            try
            {
                connection = new SqlConnection(@WebConfigurationManager.ConnectionStrings[connString].ToString());

                if (connection.State.ToString() != "Open")
                {
                    connection.Open();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Close()
        {
            try
            {
                connection.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string insertAccount(User u)
        {

            string passw = FormsAuthentication.HashPasswordForStoringInConfigFile(u.Password, "SHA1");

            string insertCmdString = "INSERT INTO Users(NAME, SURNAME, NICKNAME, BIRTHDATE, EMAIL, AVATAR, PASSWORD, STATUS) "
                        + " VALUES('" + u.Name + "', '" + u.Surname + "', '" + u.Nickname + "', CONVERT(smalldatetime,'" + u.BirthDate.ToString().Substring(0, 10) + "',104), '"
                        + u.Email + "',@Avatar,'" + passw + "', '" + u.Status + "');";

            SqlParameter param = new SqlParameter("@Avatar", SqlDbType.Binary);

            if (u.Avatar != null)
            {
                param.Value = u.Avatar;
            }
            else
            {
                param.Value = DBNull.Value;
            }

            SqlCommand insertCmd = new SqlCommand(insertCmdString, connection);
            SqlTransaction transaction2 = connection.BeginTransaction("SampleTransaction");
            insertCmd.Parameters.Add(param);

            insertCmd.Transaction = transaction2;

            try
            {
                insertCmd.ExecuteNonQuery();
                transaction2.Commit();

                return "Success";
            }
            catch
            {		
                transaction2.Rollback();

                return "Fail";
            }
        }

        internal string AddAccount(User u)
        {
            string checkCmdString = "SELECT * FROM Users WHERE NICKNAME = '" + u.Nickname
                + "' OR EMAIL = '" + u.Email + "';";

            SqlCommand checkCmd = new SqlCommand(checkCmdString, connection);
            SqlTransaction transaction1 = connection.BeginTransaction("SampleTransaction");

            checkCmd.Transaction = transaction1;

            try
            {
                checkCmd.ExecuteNonQuery();
                transaction1.Commit();

                SqlDataReader reader = checkCmd.ExecuteReader();
                reader.Read();
                bool isOk = !(reader.HasRows);
                reader.Close();

                if (!isOk)
                {
                    return "Duplicate";
                }
                else
                {
                    string toReturn = insertAccount(u);

                    return toReturn;
                }
            }
            catch
            {
                transaction1.Rollback();
                return "Fail";
            }
        }

        internal string getNickname(string email)
        {
            string toReturn = "";
            string cmdString = "SELECT * FROM Users WHERE EMAIL='" + email + "';";

            using (SqlCommand cmd = new SqlCommand(cmdString, connection))
            {
                SqlTransaction transaction = connection.BeginTransaction("PasswordRequest");

                cmd.Transaction = transaction;

                try
                {
                    cmd.ExecuteNonQuery();
                    transaction.Commit();

                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        toReturn = reader["NICKNAME"].ToString();
                    }

                    reader.Close();
                }
                catch
                {
                    transaction.Rollback();
                }
            }

            return toReturn;
        }

        internal List<User> getUsers()
        {
            try
            {
                List<User> toReturn = new List<User>();

                string cmdString = "SELECT * FROM Users;";

                using (SqlCommand cmd = new SqlCommand(cmdString, connection))
                {
                    SqlTransaction transaction = connection.BeginTransaction("GetUsers");
                    cmd.Transaction = transaction;

                    try
                    {
                        cmd.ExecuteNonQuery();
                        transaction.Commit();

                        SqlDataReader reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            User user = new User();

                            user.Name = reader["NAME"].ToString();
                            user.Surname = reader["SURNAME"].ToString();
                            user.Nickname = reader["NICKNAME"].ToString();
                            user.BirthDate = Convert.ToDateTime(reader["BIRTHDATE"].ToString());
                            user.Password = reader["PASSWORD"].ToString();
                            user.Status = reader["STATUS"].ToString();
                            user.Email = reader["EMAIL"].ToString();

                            if (reader["AVATAR"] != DBNull.Value)
                            {
                                user.Avatar = (byte[])(reader["AVATAR"]);
                            }
                            else
                            {
                                user.Avatar = null;
                            }

                            toReturn.Add(user);
                        }
                    }
                    catch
                    {
                        transaction.Rollback();
                    }
                }

                return toReturn;
            }
            catch
            {
                return new List<User>();
            }
        }

        internal bool isEmailSent(string email)
        {
            bool toReturn = false;

            SqlCommand cmd = new SqlCommand("ResetPassword", connection);
            cmd.CommandType = CommandType.StoredProcedure;

            string nickname = getNickname(email);

            SqlParameter paramUsername = new SqlParameter("@UserName", nickname);

            cmd.Parameters.Add(paramUsername);

            SqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
                {
                    if (Convert.ToBoolean(rdr["ReturnCode"]))
                    {
                        toReturn = SendPasswordResetLetter(rdr["Email"].ToString(), nickname, rdr["UniqueId"].ToString());
                    }
            }

            return toReturn;
        }

        private bool ExecuteSP(string SPName, List<SqlParameter> SPParameters)
        {
                SqlCommand cmd = new SqlCommand(SPName, connection);
                cmd.CommandType = CommandType.StoredProcedure;

                foreach (SqlParameter parameter in SPParameters)
                {
                    cmd.Parameters.Add(parameter);
                }

                return Convert.ToBoolean(cmd.ExecuteScalar());
        }

        internal bool IsPasswordResetLinkValid(string queryString)
        {
            List<SqlParameter> paramList = new List<SqlParameter>()
              {
                    new SqlParameter()
                 {
                    ParameterName = "@GUID",
                    Value = queryString
                 }
              };

            return ExecuteSP("IsResetLinkValid", paramList);
        }

        internal bool ChangeUserPassword(string queryString, string password)
        {
            List<SqlParameter> paramList = new List<SqlParameter>()
               {
                 new SqlParameter()
                    {
                         ParameterName = "@GUID",
                         Value = queryString
                    },
                new SqlParameter()
                     {
                         ParameterName = "@Password",
                         Value = FormsAuthentication.HashPasswordForStoringInConfigFile(password, "SHA1")
                     }
                };

            return ExecuteSP("ChangePassword", paramList);
        }


        private bool SendPasswordResetLetter(string toMail, string username, string uniqueid)
        {
            string smtpHost = "smtp.gmail.com";
            int smtpPort = 587;
            string smtpUserName = "btsemail1@gmail.com";
            string smtpUserPass = "btsadmin";

            SmtpClient client = new SmtpClient(smtpHost, smtpPort);
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(smtpUserName, smtpUserPass);
            client.EnableSsl = true;

            string msgFrom = smtpUserName;
            string msgTo = toMail;
            string msgSubject = "Password Notification";

            string msgBody = "Dear " + username + ", <br/><br/>";
            msgBody += "Please follow this link: http://its.local/Bts/ChangePassword?uid=" + uniqueid + " to change your password";
            MailMessage message = new MailMessage(msgFrom, msgTo, msgSubject, msgBody);

            message.IsBodyHtml = true;

            try
            {
                client.Send(message);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal bool isAuthenticated(string nickname, string password)
        {
            string encryptedPassword = FormsAuthentication.HashPasswordForStoringInConfigFile(password, "SHA1");

            string cmdString = "SELECT * FROM Users WHERE NICKNAME='" + nickname + 
                "' AND PASSWORD='" + encryptedPassword + "';";

            using (SqlCommand cmd = new SqlCommand(cmdString, connection))
            {
                SqlTransaction transaction = connection.BeginTransaction("FindUser");
                cmd.Transaction = transaction;

                try
                {
                    cmd.ExecuteNonQuery();
                    transaction.Commit();

                    SqlDataReader reader = cmd.ExecuteReader();

                    if(reader.HasRows)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch
                {
                    transaction.Rollback();
                    return false;
                }
            }
        }
    }
}
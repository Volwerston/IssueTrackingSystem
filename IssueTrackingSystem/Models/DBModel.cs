using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Configuration;
using IssueTrackingSystem.Common;
using System.Web.Security;
using System.Globalization;

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
            catch(Exception ex)
            {
                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.Message);
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

            string insertCmdString = "INSERT INTO Users(NAME, SURNAME, NICKNAME, BIRTHDATE, EMAIL, AVATAR, PASSWORD, STATUS, Confirmed) "
                        + " VALUES('" + u.Name + "', '" + u.Surname + "', '" + u.Nickname + "', CONVERT(smalldatetime,'" + u.BirthDate.ToString().Substring(0, 10) + "',104), '"
                        + u.Email + "',@Avatar,'" + passw + "', '" + u.Status + "', 0);";

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
            catch(Exception ex)
            {		
                transaction2.Rollback();

                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.Message);

                return "Fail";
            }
        }

        internal bool ConfirmUser(int userId)
        {
            bool toReturn = false;

            string cmdString = "UPDATE Users SET Confirmed=1 WHERE ID=" + userId + ";";

            SqlCommand cmd = new SqlCommand(cmdString, connection);
            SqlTransaction transaction = connection.BeginTransaction("UserConfirmTransaction");
            cmd.Transaction = transaction;

            try
            {
                cmd.ExecuteNonQuery();
                transaction.Commit();
                toReturn = true;
            }
            catch(Exception ex)
            {
                toReturn = false;
                transaction.Rollback();

                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.ToString());
            }

            return toReturn;
        }

        internal string AddAccount(User u)
        {
            string checkCmdString = "SELECT * FROM Users WHERE NICKNAME = '" + u.Nickname
                + "' OR EMAIL = '" + u.Email + "';";

            SqlCommand checkCmd = new SqlCommand(checkCmdString, connection);
            SqlTransaction transaction1 = connection.BeginTransaction("SampleTransaction");
            SqlDataReader reader = null;

            checkCmd.Transaction = transaction1;

            try
            {
                reader = checkCmd.ExecuteReader();

                bool isOk = !(reader.HasRows);
                reader.Close();
                transaction1.Commit();

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
            catch(Exception ex)
            {
                reader.Close();
                transaction1.Rollback();

                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.Message);
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
                SqlDataReader reader = null;
                cmd.Transaction = transaction;

                try
                {
                    reader = cmd.ExecuteReader();

                        if (reader.Read())
                        {
                            toReturn = reader["NICKNAME"].ToString();
                        }
                    
                        reader.Close();
                        transaction.Commit();
                }
                catch(Exception ex)
                {
                    reader.Close();
                    transaction.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.Message);
                }
            }

            return toReturn;
        }

        internal List<User> getUsers()
        {
                List<User> toReturn = new List<User>();

                string cmdString = "SELECT * FROM Users;";

                using (SqlCommand cmd = new SqlCommand(cmdString, connection))
                {
                    SqlTransaction transaction = connection.BeginTransaction("ExtractUsers");
                    cmd.Transaction = transaction;
                SqlDataReader reader = null;

                    try
                    {

                    reader = cmd.ExecuteReader();

                            while (reader.Read())
                            {
                                User user = new User();

                                user.Id = Convert.ToInt32(reader["ID"].ToString());
                                user.Name = reader["NAME"].ToString();
                                user.Surname = reader["SURNAME"].ToString();
                                user.Nickname = reader["NICKNAME"].ToString();
                                user.BirthDate = Convert.ToDateTime(reader["BIRTHDATE"].ToString());
                                user.Password = reader["PASSWORD"].ToString();
                                user.Status = reader["STATUS"].ToString();
                                user.Email = reader["EMAIL"].ToString();

                            if (reader["Confirmed"] != DBNull.Value)
                            {
                                user.Confirmed = Convert.ToBoolean(reader["Confirmed"].ToString());
                            }
                            else user.Confirmed = false;

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

                        reader.Close();
                    transaction.Commit();

                    }
                    catch(Exception ex)
                    {
                     reader.Close();
                        transaction.Rollback();

                        ErrorTracker tracker = new ErrorTracker();
                        tracker.LogError(ex.Message);
                    }
                }

                return toReturn;
            }

        internal bool isEmailSent(string email)
        {
            bool toReturn = false;

            SqlCommand cmd = new SqlCommand("ResetPassword", connection);
            cmd.CommandType = CommandType.StoredProcedure;

            string nickname = getNickname(email);

            SqlParameter paramUsername = new SqlParameter("@UserName", nickname);

            cmd.Parameters.Add(paramUsername);

            SqlTransaction transaction = connection.BeginTransaction("PasswordResetTransaction");
            cmd.Transaction = transaction;
            SqlDataReader rdr = null;

            try
            {
                rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    if (Convert.ToBoolean(rdr["ReturnCode"]))
                    {
                        toReturn = SendPasswordResetLetter(rdr["Email"].ToString(), nickname, rdr["UniqueId"].ToString());
                    }
                }

                rdr.Close();
                transaction.Commit();
            }
            catch(Exception ex)
            {
                rdr.Close();
                transaction.Rollback();

                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.ToString());
            }

            return toReturn;
        }

        internal bool AddProject(Project proj)
        {
            bool toReturn = false;

            string cmdString = "INSERT INTO Projects (NAME, DESCRIPTION, LOGO, PmId) VALUES(@Name, @Descr, @Logo, @PmId);";


            SqlCommand cmd = new SqlCommand(cmdString, connection);
            SqlTransaction transaction = connection.BeginTransaction("ProjectAddTransaction");
            cmd.Transaction = transaction;

            cmd.Parameters.AddWithValue("@Name", proj.Name);
            cmd.Parameters.AddWithValue("@Descr", proj.Description);
            cmd.Parameters.AddWithValue("@Logo", proj.Logo);
            cmd.Parameters.AddWithValue("@PmId", proj.PmId);

            try
            {
                cmd.ExecuteNonQuery();
                transaction.Commit();
                toReturn = true;
            }
            catch(Exception ex)
            {
                toReturn = false;
                transaction.Rollback();

                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.ToString());
            }

            return toReturn;
        }

        internal bool AddAttachments(List<HttpPostedFileBase> attachments, int id)
        {
            if (id != -1)
            {
                bool allSucceeded = true;
                SqlTransaction transaction = connection.BeginTransaction("InsertAttachments");

                foreach (HttpPostedFileBase attachment in attachments)
                {
                    if (attachment != null)
                    {
                        string cmdString = "INSERT INTO Attachments (NAME, DATA, BUG_ID) VALUES(@Name, @Data, @BugId)";
                        SqlCommand cmd = new SqlCommand(cmdString, connection);
                        cmd.Transaction = transaction;

                        byte[] file = new byte[attachment.ContentLength];
                        attachment.InputStream.Read(file, 0, attachment.ContentLength);

                        SqlParameter paramName = new SqlParameter("@Name", attachment.GetType().ToString());
                        SqlParameter paramBugId = new SqlParameter("@BugId", id);
                        SqlParameter paramData = new SqlParameter("@Data", SqlDbType.Binary);
                        paramData.Value = file;

                        cmd.Parameters.Add(paramName);
                        cmd.Parameters.Add(paramData);
                        cmd.Parameters.Add(paramBugId);

                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch(Exception ex)
                        {
                            allSucceeded = false;

                            ErrorTracker tracker = new ErrorTracker();
                            tracker.LogError(ex.Message);
                            break;
                        }
                    }
                }

                if (allSucceeded)
                {
                    transaction.Commit();
                }
                else
                {
                    transaction.Rollback();
                }

                return allSucceeded;
            }
            else
            {
                return false;
            }
        }

        internal void ApproveDeveloperForProject(string projectName, int userId)
        {
            string cmdString = "UPDATE ProjectDeveloper SET Approved=1 WHERE PROJ_NAME='" + projectName + "' AND DEV_ID=" + userId + ";";
            SqlCommand cmd = new SqlCommand(cmdString, connection);
            SqlTransaction transaction = connection.BeginTransaction("DevApproveTransaction");
            cmd.Transaction = transaction;

            try
            {
                cmd.ExecuteNonQuery();
                transaction.Commit();
            }
            catch(Exception ex)
            {
                transaction.Rollback();
                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.ToString());
            }
        }

        internal int GetBugId(string subject)
        {
            int toReturn = -1;
            string cmdString = "SELECT ID FROM Bugs WHERE SUBJECT='" + subject + "'";

            SqlCommand cmd = new SqlCommand(cmdString, connection);
            SqlTransaction transaction = connection.BeginTransaction("GetBugId");
            cmd.Transaction = transaction;
            SqlDataReader rdr = null;

            try
            {
                rdr = cmd.ExecuteReader();

                if (rdr.Read())
                {
                    toReturn = Convert.ToInt32(rdr["ID"].ToString());
                }
                rdr.Close();
                transaction.Commit();

            }
            catch(Exception ex)
            {
                rdr.Close();
                transaction.Rollback();

                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.Message);
            }

            return toReturn;
        }

        internal List<User> searchForUsers(int id,List<string> userNames, List<string> userStatuses)
        {
            List<User> toReturn = null;
            string cmdString = null;

            if(userNames.Count == 0 && userStatuses == null)
            {
                cmdString = "";
            }
            else if(userNames.Count == 0 && userStatuses != null )
            {
                cmdString = "SELECT TOP 5 * FROM Users WHERE ID>" + id + " AND(";

                foreach(string status in userStatuses)
                {
                    cmdString += "STATUS='" + status + "' OR ";
                }

                cmdString = cmdString.Substring(0, cmdString.Length - 4);
                cmdString += ");";
            }
            else if(userNames.Count != 0 && userStatuses == null)
            {
                cmdString = "SELECT TOP 5 * FROM Users WHERE ID>" + id + " AND(";

                foreach (string name in userNames)
                {
                    cmdString += "(Upper(NAME)='" + name.ToUpper() + "' OR Upper(SURNAME)='" + name.ToUpper() + "' OR Upper(NICKNAME)='" + name.ToUpper() + "') OR ";
                }

                cmdString = cmdString.Substring(0, cmdString.Length - 4);
                cmdString += ");";
            }
            else
            {
                cmdString = "SELECT TOP 5 * FROM Users WHERE ID>" + id + " AND ((";

                foreach (string name in userNames)
                {
                    cmdString += "(Upper(NAME)='" + name.ToUpper() + "' OR Upper(SURNAME)='" + name.ToUpper() + "' OR Upper(NICKNAME)='" + name.ToUpper() + "') OR ";
                }

                cmdString = cmdString.Substring(0, cmdString.Length - 4);
                cmdString += ") AND (";

                foreach (string status in userStatuses)
                {
                    cmdString += "STATUS='" + status + "' OR ";
                }

                cmdString = cmdString.Substring(0, cmdString.Length - 4);

                cmdString += "));";
            }

            if(cmdString != "")
            {
                SqlCommand cmd = new SqlCommand(cmdString, connection);
                SqlTransaction transaction = connection.BeginTransaction("UserSearch");
                cmd.Transaction = transaction;
                SqlDataReader reader = null;

                try
                {
                    reader = cmd.ExecuteReader();

                    toReturn = new List<User>();

                        while (reader.Read())
                        {
                            User user = new User();

                            user.Id = Convert.ToInt32(reader["ID"].ToString());
                            user.Name = reader["NAME"].ToString();
                            user.Surname = reader["SURNAME"].ToString();
                            user.Nickname = reader["NICKNAME"].ToString();
                            user.BirthDate = Convert.ToDateTime(reader["BIRTHDATE"].ToString());
                            user.Password = reader["PASSWORD"].ToString();
                            user.Status = reader["STATUS"].ToString();
                            user.Email = reader["EMAIL"].ToString();

                            if (reader["Confirmed"] != DBNull.Value)
                            {
                                user.Confirmed = Convert.ToBoolean(reader["Confirmed"].ToString());
                            }
                            else user.Confirmed = false;

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

                        reader.Close();
                        transaction.Commit();
                }
                catch(Exception ex)
                {
                    reader.Close();
                    transaction.Rollback();
                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.Message);
                }
            }

            if(toReturn != null && toReturn.Count() == 0)
            {
                toReturn = null;
            }

            return toReturn;
        }

        internal List<Notification> GetNotificationsOfUser(string receiver)
        {
            List<Notification> toReturn = null;

            string cmdString = "SELECT * FROM Notifications WHERE RECEIVER='" + receiver + "';";
            SqlCommand cmd = new SqlCommand(cmdString, connection);
            SqlTransaction transaction = connection.BeginTransaction("UserNotificationsTransaction");
            cmd.Transaction = transaction;
            SqlDataReader rdr = null;

            try
            {
                rdr = cmd.ExecuteReader();

                    toReturn = new List<Notification>();

                    while (rdr.Read())
                    {
                        Notification n = new Notification();

                        n.Sender = rdr["SENDER"].ToString();
                        n.Receiver = rdr["RECEIVER"].ToString();
                        n.SendDate = Convert.ToDateTime(rdr["SEND_TIME"].ToString());
                        n.Message = rdr["MESSAGE"].ToString();

                        toReturn.Add(n);
                    }

                    rdr.Close();
                    transaction.Commit();
                
            }
            catch(Exception ex)
            {
                rdr.Close();
                transaction.Rollback();
                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.ToString());
            }

            if(toReturn != null && toReturn.Count() == 0)
            {
                toReturn = null;
            }

            return toReturn;
        }

        internal bool RemoveDevsFromProject(string projName, List<int> toErase)
        {
            bool toReturn = false;

            string cmdString = "DELETE FROM ProjectDeveloper WHERE";

            foreach(int eraseId in toErase)
            {
                cmdString += " DEV_ID=" + eraseId + " OR";
            }

            cmdString = cmdString.Substring(0, cmdString.Length - 3);
            cmdString += ";";

            cmdString += " UPDATE Bugs SET DEVELOPER_ID=0 WHERE ";

            foreach (int eraseId in toErase)
            {
                cmdString += " DEVELOPER_ID=" + eraseId + " OR";
            }

            cmdString = cmdString.Substring(0, cmdString.Length - 3);
            cmdString += ";";

            SqlCommand cmd = new SqlCommand(cmdString, connection);
            SqlTransaction transaction = connection.BeginTransaction("EraseDevsTransaction");
            cmd.Transaction = transaction;

            try
            {
                cmd.ExecuteNonQuery();
                transaction.Commit();
                toReturn = true;
            }
            catch(Exception ex)
            {
                toReturn = false;
                transaction.Rollback();
                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.ToString());
            }

            return toReturn;
        }

        internal bool InviteDeveloperToProject(string projectName, int devId)
        {
            bool toReturn = false;

            string cmdString = "INSERT INTO ProjectDeveloper(PROJ_NAME, DEV_ID, Approved) VALUES('" + projectName + "', " + devId + ", 0);";
            SqlCommand cmd = new SqlCommand(cmdString, connection);
            SqlTransaction transaction = connection.BeginTransaction("InsertDevTransaction");
            cmd.Transaction = transaction;

            try
            {
                cmd.ExecuteNonQuery();
                transaction.Commit();
                toReturn = true;
            }
            catch(Exception ex)
            {
                toReturn = false;
                transaction.Rollback();

                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.ToString());
            }

            return toReturn;
        }

        internal bool EditUserEmail(int id, string email)
        {
            bool toReturn = false;

            string cmdString = "UPDATE Users SET EMAIL='" + email + "' WHERE ID=" + id + ";";
            SqlCommand cmd = new SqlCommand(cmdString, connection);
            SqlTransaction transaction = connection.BeginTransaction("EditEmail");
            cmd.Transaction = transaction;

            try
            {
                cmd.ExecuteNonQuery();
                transaction.Commit();
                toReturn = true;
            }
            catch(Exception ex)
            {
                transaction.Rollback();

                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.Message);
            }

            return toReturn;
        }

        internal bool EditUserBirthDate(int id, string birthdate)
        {
            bool toReturn = false;

            string cmdString = "UPDATE Users SET BIRTHDATE=CONVERT(smalldatetime, '" + birthdate + "', 104) WHERE ID=" + id + ";";
            SqlCommand cmd = new SqlCommand(cmdString, connection);
            SqlTransaction transaction = connection.BeginTransaction("EditBirthDate");
            cmd.Transaction = transaction;

            try
            {
                cmd.ExecuteNonQuery();
                transaction.Commit();
                toReturn = true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.Message);
            }

            return toReturn;
        }

        internal bool ReportBug(Bug b)
        {
            bool toReturn = false;

            SqlCommand cmd = new SqlCommand("addBug", connection);
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter subject = new SqlParameter("@Subject", b.Subject);
            SqlParameter description = new SqlParameter("@Description", b.Description);
            SqlParameter projId = new SqlParameter("@ProjectId", b.ProjectId);
            SqlParameter status = new SqlParameter("@Status", b.Status);
            SqlParameter topicStarter = new SqlParameter("@TopicStarter", b.TopicStarter);

            cmd.Parameters.Add(subject);
            cmd.Parameters.Add(description);
            cmd.Parameters.Add(projId);
            cmd.Parameters.Add(status);
            cmd.Parameters.Add(topicStarter);

            SqlTransaction transaction = connection.BeginTransaction("AddNewBug");
            cmd.Transaction = transaction;

            try
            {
                if((int)cmd.ExecuteScalar() == 1)
                {
                    toReturn = true;
                }

                transaction.Commit();
            }
            catch(Exception ex)
            {
                transaction.Rollback();

                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.Message);
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

            SqlTransaction transaction = connection.BeginTransaction("ExecuteCommand");
            cmd.Transaction = transaction;

            try
            {
                bool toReturn = Convert.ToBoolean(cmd.ExecuteScalar());
                transaction.Commit();
                return toReturn;
            }
            catch(Exception ex)
            {
                transaction.Rollback();

                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.Message);
                return false;
            }
        }

        internal void deleteExpiredRecords()
        {
            SqlCommand cmd = new SqlCommand("DeleteExpiredRecords", connection);
            cmd.CommandType = CommandType.StoredProcedure;

            SqlTransaction transaction = connection.BeginTransaction("ExpiredRecordsDeleting");

            cmd.Transaction = transaction;

            try
            {
                cmd.ExecuteNonQuery();
                transaction.Commit();
            }
            catch(Exception ex)
            {
                transaction.Rollback();

                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.Message);
            }
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
            catch(Exception ex)
            {
                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.Message);

                return false;
            }
        }

        internal User getAuthenticatedUser(string nickname, string password)
        {
            User toReturn = new User();
            toReturn.Id = -1;
            SqlDataReader reader = null;

            string encryptedPassword = FormsAuthentication.HashPasswordForStoringInConfigFile(password, "SHA1");

            string cmdString = "SELECT * FROM Users WHERE NICKNAME='" + nickname +
                "' AND PASSWORD='" + encryptedPassword + "';";

            using (SqlCommand cmd = new SqlCommand(cmdString, connection))
            {
                SqlTransaction transaction = connection.BeginTransaction("FindUserTransaction");
                cmd.Transaction = transaction;

                try
                {
                    reader = cmd.ExecuteReader();

                        if (reader.Read())
                        {
                            toReturn.Id = Convert.ToInt32(reader["ID"].ToString());
                            toReturn.Status = reader["STATUS"].ToString();

                            if (reader["Confirmed"] != DBNull.Value)
                            {
                                toReturn.Confirmed = Convert.ToBoolean(reader["Confirmed"].ToString());
                            }
                            else toReturn.Confirmed = false;
                        }

                        reader.Close();
                        transaction.Commit();

                }
                catch (Exception ex)
                {
                    reader.Close();
                    transaction.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.Message);
                }
            }

            return toReturn;
        }

        internal void WriteMessage(string To, string From, string message)
        {
            string cmdString = "INSERT INTO Notifications(RECEIVER, SENDER, MESSAGE, SEND_TIME) VALUES('" + To + "', '" + From + "','"
             + message + "', CONVERT(smalldatetime, GETDATE(), 104));";

            SqlCommand cmd = new SqlCommand(cmdString, connection);
            SqlTransaction transaction = connection.BeginTransaction("NotificationAddTransaction");
            cmd.Transaction = transaction;

            try
            {
                cmd.ExecuteNonQuery();
                transaction.Commit();
            }
            catch(Exception ex)
            {
                transaction.Rollback();

                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.ToString());
            }
        }

        internal bool SetDevIdForBug(int bugId, int id)
        {
            bool toReturn = false;

            string cmdString = "UPDATE Bugs SET DEVELOPER_ID=" + id + " WHERE ID=" + bugId + ";";
            SqlCommand cmd = new SqlCommand(cmdString, connection);
            SqlTransaction transaction = connection.BeginTransaction("AlterDevId");
            cmd.Transaction = transaction;

            try
            {
                cmd.ExecuteNonQuery();
                transaction.Commit();
                toReturn = true;
            }
            catch
            {
                toReturn = false;
                transaction.Rollback();
            }

            return toReturn;
        }

        internal void AddMessageToWorkflow(int bugId, string messageToAdd, string nickName)
        {
            string cmdString = "INSERT INTO Messages (MESSAGE, ADD_TIME, SENDER_NICKNAME, BUG_ID, CORRECT) VALUES('" + messageToAdd + "',"
                + "CONVERT(smalldatetime, GETDATE(), 104), '" + nickName + "', " + bugId + ", 0);";

            SqlCommand cmd = new SqlCommand(cmdString, connection);
            SqlTransaction transaction = connection.BeginTransaction("MessageAddTransaction");
            cmd.Transaction = transaction;

            try
            {
                cmd.ExecuteNonQuery();
                transaction.Commit();
            }
            catch(Exception ex)
            {
                transaction.Rollback();

                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.ToString());
            }
        }

        internal bool MarkRightIssueAnswer(int bugId, int selectedItemId, int estimate)
        {
            bool toReturn = false;

            string cmdString = "UPDATE Messages SET CORRECT=1 WHERE ID=" + selectedItemId +
                "; UPDATE Bugs SET ESTIMATE=" + estimate + ", STATUS='Closed' WHERE ID=" + bugId + ";";

            SqlCommand cmd = new SqlCommand(cmdString, connection);
            SqlTransaction transaction = connection.BeginTransaction("RightAnswerTransaction");
            cmd.Transaction = transaction;

            try
            {
                cmd.ExecuteNonQuery();
                transaction.Commit();
                toReturn = true;
            }
            catch(Exception ex)
            {
                toReturn = false;
                transaction.Rollback();

                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.ToString());
            }


            return toReturn;
        }

        public List<User> GetDevelopersOfProject(string projName, out List<User> invitedDevelopers)
        {
            List<User> toReturn = null;
            invitedDevelopers = new List<User>();

            string cmdString = "SELECT A.ID, A.NAME, A.SURNAME, A.NICKNAME, A.STATUS, A.Confirmed, B.Approved "
                + "FROM Users A inner join ProjectDeveloper B ON A.ID = B.DEV_ID "
                + "WHERE B.PROJ_NAME='" + projName + "';";

            SqlCommand cmd = new SqlCommand(cmdString, connection);
            SqlTransaction transaction = connection.BeginTransaction("ExtractProjectDevelopers");
            cmd.Transaction = transaction;
            SqlDataReader rdr = null;

            try
            {
                rdr = cmd.ExecuteReader();

                toReturn = new List<User>();

                    while (rdr.Read())
                    {
                        User u = new User();

                        u.Id = Convert.ToInt32(rdr["ID"].ToString());
                        u.Name = rdr["NAME"].ToString();
                        u.Surname = rdr["SURNAME"].ToString();
                        u.Nickname = rdr["NICKNAME"].ToString();
                        u.Status = rdr["STATUS"].ToString();

                        if (rdr["Confirmed"] != DBNull.Value)
                        {
                            u.Confirmed = Convert.ToBoolean(rdr["Confirmed"].ToString());
                        }
                        else
                        {
                            u.Confirmed = false;
                        }

                        if (u.Confirmed)
                        {
                            if (rdr["Approved"] != DBNull.Value)
                            {
                                if (Convert.ToBoolean(rdr["Approved"].ToString()))
                                {
                                    toReturn.Add(u);
                                }
                                else
                                {
                                    invitedDevelopers.Add(u);
                                }
                            }
                            else
                            {
                                invitedDevelopers.Add(u);
                            }
                        }
                    }

            rdr.Close();
            transaction.Commit();
        }
            catch(Exception ex)
            {
                rdr.Close();
                transaction.Rollback();
                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.ToString());
            }

            if(toReturn != null && toReturn.Count() == 0)
            {
                toReturn = null;
            }

            return toReturn;
        }

        internal bool AddSolutionOfBug(int bugId, string solution)
        {
            bool toReturn = false;

            string cmdString = "UPDATE Bugs SET Solution='" + solution + "' WHERE ID=" + bugId + ";";

            SqlCommand cmd = new SqlCommand(cmdString, connection);
            SqlTransaction transaction = connection.BeginTransaction("SolutionAddTransaction");
            cmd.Transaction = transaction;

            try
            {
                cmd.ExecuteNonQuery();
                transaction.Commit();
                toReturn = true;
            }
            catch(Exception ex)
            {
                transaction.Rollback();
                toReturn = false;

                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.ToString());
            }

            return toReturn;
        }

        internal User getAuthenticatedUser(string nickname)
        {
            User toReturn = new User();
            toReturn.Id = -1;

            string cmdString = "SELECT * FROM Users WHERE NICKNAME='" + nickname + "';";

            using (SqlCommand cmd = new SqlCommand(cmdString, connection))
            {
                SqlTransaction transaction = connection.BeginTransaction("FindUser");
                cmd.Transaction = transaction;

                SqlDataReader reader = null;

                try
                {
                    reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        toReturn.Id = Convert.ToInt32(reader["ID"].ToString());
                        toReturn.Name = reader["NAME"].ToString();
                        toReturn.Surname = reader["SURNAME"].ToString();
                        toReturn.Nickname = reader["NICKNAME"].ToString();
                        toReturn.Email = reader["EMAIL"].ToString();
                        toReturn.BirthDate = Convert.ToDateTime(reader["BIRTHDATE"].ToString());
                        toReturn.Status = reader["STATUS"].ToString();
                        toReturn.Avatar = (byte[])reader["AVATAR"];

                        if (reader["Confirmed"] != DBNull.Value)
                        {
                            toReturn.Confirmed = Convert.ToBoolean(reader["Confirmed"].ToString());
                        }
                        else toReturn.Confirmed = false;
                    }

                    reader.Close();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    reader.Close();
                    transaction.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.Message);
                }
            }

            return toReturn;
        }

        public bool EditUserAvatar(int id,byte[] avatar)
        {
            bool toReturn = false;

            string cmdString = "UPDATE Users SET AVATAR=@Avatar WHERE ID=" + id + ";";

            SqlParameter param = new SqlParameter("@Avatar", SqlDbType.Binary);

            if (avatar != null)
            {
                param.Value = avatar;
            }
            else
            {
                param.Value = DBNull.Value;
            }

            SqlCommand cmd = new SqlCommand(cmdString, connection);
            cmd.Parameters.Add(param);
            SqlTransaction transaction = connection.BeginTransaction("EditAvatar");
            cmd.Transaction = transaction;

            try
            {
                cmd.ExecuteNonQuery();
                transaction.Commit();

                toReturn = true;
            }
            catch
            {
                transaction.Rollback();
            }

            return toReturn;
        }

        public List<Project> GetProjectsByName(string name)
        {
            List<Project> toReturn = new List<Project>();

            if (name != null)
            {
                string cmdString = "SELECT * FROM Projects WHERE NAME = '" + name + "';";

                SqlCommand cmd = new SqlCommand(cmdString, connection);

                SqlTransaction transaction = connection.BeginTransaction("FindByTitle");
                cmd.Transaction = transaction;
                SqlDataReader reader = null;

                try
                {
                    reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        Project project = new Project();
                        project.Id = Convert.ToInt32(reader["ID"].ToString());
                        project.Description = reader["DESCRIPTION"].ToString();
                        project.Name = reader["NAME"].ToString();
                        project.PmId = int.Parse(reader["PmId"].ToString());

                        if(reader["LOGO"] != DBNull.Value)
                        {
                            project.Logo = (byte[])reader["LOGO"];
                        }

                        toReturn.Add(project);
                    }

                    reader.Close();
                    transaction.Commit();

                }
                catch(Exception ex)
                {
                    reader.Close();
                    transaction.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.Message);
                }
            }

            return toReturn;
        }

        public List<Project> GetProjectsByCategories(int[] categories)
        {
            List<Project> toReturn = new List<Project>();


            if (categories != null)
            {
                string cmdString = "SELECT TOP 5 A.ID, A.NAME, A.DESCRIPTION, A.PmId, A.LOGO" +
                                    " FROM Projects A inner join ProjectCategory B on A.ID = B.PROJECT_ID" +
                                    " WHERE A.ID > " + categories[categories.Length - 1] + " AND ( ";

                for(int i = 0; i < categories.Length - 1; ++i)
                {
                    cmdString += "B.CATEGORY_ID = " + categories[i] + " OR ";
                }

                cmdString = cmdString.Substring(0, cmdString.Length - 4);

                cmdString += ");";

                SqlCommand cmd = new SqlCommand(cmdString, connection);
                SqlTransaction transaction = connection.BeginTransaction("Categories");
                cmd.Transaction = transaction;

                try
                { 
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                        foreach (DataRow row in table.Rows)
                        {
                            Project project = new Project();
                            project.Id = Convert.ToInt32(row["ID"].ToString());
                            project.Description = row["DESCRIPTION"].ToString();
                            project.Name = row["NAME"].ToString();
                            project.PmId = int.Parse(row["PmId"].ToString());

                            if (row["LOGO"] != DBNull.Value)
                            {
                                project.Logo = (byte[])row["LOGO"];
                            }

                            toReturn.Add(project);
                        }

                    transaction.Commit();
                }
                catch(Exception ex)
                {
                    transaction.Rollback();

                    ErrorTracker tracker = new ErrorTracker();
                    tracker.LogError(ex.Message);
                }
            }

            toReturn = toReturn.GroupBy(x => x.Id).Select(group => group.First()).ToList();

            return toReturn;
        }

        public List<Bug> GetProjectBugs(Project proj)
        {

            List<Bug> toReturn = new List<Bug>();

            string cmdString = "SELECT * FROM Bugs WHERE PROJECT_ID = '" + proj.Id + "';";

            SqlCommand cmd = new SqlCommand(cmdString, connection);

            SqlTransaction transaction = connection.BeginTransaction("BugsOfProject");

            cmd.Transaction = transaction;

            SqlDataReader rdr = null;

            try
            {
               rdr = cmd.ExecuteReader();


                    while (rdr.Read())
                    {
                        Bug bug = new Bug();

                        bug.Id = Convert.ToInt32(rdr["ID"].ToString());
                        bug.Subject = rdr["SUBJECT"].ToString();
                        bug.Description = rdr["DESCRIPTION"].ToString();
                        bug.Status = rdr["STATUS"].ToString();
                        bug.TopicStarter = rdr["TOPIC_STARTER"].ToString();

                        if (rdr["DEVELOPER_ID"] != DBNull.Value)
                        {
                            bug.DeveloperId = Convert.ToInt32(rdr["DEVELOPER_ID"].ToString());
                        }

                        if (rdr["PM_ID"] != DBNull.Value)
                        {
                            bug.PmId = Convert.ToInt32(rdr["PM_ID"].ToString());
                        }

                        if (rdr["ESTIMATE"] != DBNull.Value)
                        {
                            bug.Estimate = Convert.ToInt32(rdr["ESTIMATE"].ToString());
                        }

                        if(rdr["Solution"] != DBNull.Value)
                        {
                            bug.Solution = rdr["Solution"].ToString();
                        }


                        toReturn.Add(bug);
                    }

                    rdr.Close();
                transaction.Commit();
            }
            catch(Exception ex)
            {
                rdr.Close();
                transaction.Rollback();

                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.Message);
            }

            if(toReturn != null && toReturn.Count() == 0)
            {
                toReturn = null;
            }
            return toReturn;
        }

        internal List<Category> getCategories()
        {
            List<Category> toReturn = new List<Category>();

            string cmdString = "SELECT * FROM Categories;";
            SqlCommand cmd = new SqlCommand(cmdString, connection);
            SqlDataReader rdr = null;

            SqlTransaction transaction = connection.BeginTransaction("CategoriesTrasaction");
            cmd.Transaction = transaction;

            try
            {
                rdr = cmd.ExecuteReader();
                
                    while (rdr.Read())
                    {
                        Category category = new Category();
                        category.Title = rdr["TITLE"].ToString();
                        category.Id = Convert.ToInt32(rdr["ID"].ToString());
                        toReturn.Add(category);
                    }

                    rdr.Close();
                    transaction.Commit();

            }
            catch(Exception ex)
            {
                rdr.Close();
                transaction.Rollback();

                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.Message);
            }

            return toReturn;
        }

        internal List<Message> GetMessageLog(int bugId)
        {
            List<Message> toReturn = null;

            string cmdString = "SELECT * FROM Messages WHERE BUG_ID=" + bugId + ";";

            SqlCommand cmd = new SqlCommand(cmdString, connection);
            SqlTransaction transaction = connection.BeginTransaction("ExtractMessages");
            cmd.Transaction = transaction;
            SqlDataReader rdr = null;

            try
            {
                rdr = cmd.ExecuteReader();

                toReturn = new List<Message>();

                    while (rdr.Read())
                    {
                        Message toAdd = new Message();

                        toAdd.Id = Convert.ToInt32(rdr["ID"].ToString());
                        toAdd.MessageText = rdr["MESSAGE"].ToString();
                        toAdd.SenderNick = rdr["SENDER_NICKNAME"].ToString();
                        toAdd.AddingTime = Convert.ToDateTime(rdr["ADD_TIME"].ToString());
                        toAdd.BugId = Convert.ToInt32(rdr["BUG_ID"].ToString());
                        toAdd.Correct = Convert.ToBoolean(rdr["CORRECT"].ToString());

                        toReturn.Add(toAdd);
                    }

                rdr.Close();
                transaction.Commit();

            }
            catch(Exception ex)
            {
                rdr.Close();
                transaction.Rollback();
                ErrorTracker tracker = new ErrorTracker();
                tracker.LogError(ex.ToString());
            }

            if(toReturn != null && toReturn.Count == 0)
            {
                toReturn = null;
            }

            return toReturn;
        }
    }
}
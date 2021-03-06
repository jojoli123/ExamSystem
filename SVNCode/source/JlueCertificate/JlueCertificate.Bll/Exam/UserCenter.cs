﻿using JlueCertificate.Dal.MsSQL;
using JlueCertificate.Entity.Respose;
using Newtonsoft.Json;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace JlueCertificate.Bll.Exam
{
    public class UserCenter
    {
        private static SqlSugarClient db
        {
            get
            {
                ConnectionConfig connectionConfig = new ConnectionConfig()
                {
                    ConnectionString = Untity.HelperMsSQL.connStr,
                    DbType = DbType.SqlServer,//设置数据库类型
                    IsAutoCloseConnection = true,//自动释放数据务，如果存在事务，在事务结束后释放
                    InitKeyType = InitKeyType.Attribute //从实体特性中读取主键自增列信息
                };
                return new SqlSugarClient(connectionConfig);
            }
        }

        /// <summary>
        /// 登陆
        /// </summary>
        /// <param name="_examid"></param>
        /// <param name="_cardid"></param>
        /// <param name="_vcode"></param>
        /// <param name="_vcodeCookie"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static object login(string _examid, string _cardid, string _vcode, string _vcodeCookie, ref string error)
        {
            if (string.IsNullOrEmpty(_vcodeCookie))
            {
                error = "验证码已失效";
            }
            else if (_vcodeCookie != Untity.HelperSecurity.MD5(_vcode.ToLower()))
            {
                error = "验证码错误";
            }
            else
            {
                Entity.MsSQL.T_Student _student = Dal.MsSQL.T_Student.GetModelByCardId(_cardid);
                if (_student == null)
                {
                    error = "系统不存在该身份证";
                }
                else
                {
                    List<Entity.MsSQL.T_StudentTicket> _tickets = Dal.MsSQL.T_StudentTicket.GetList(_student.Id.ToString());
                    Entity.MsSQL.T_StudentTicket _ticket = _tickets.Where(ii => ii.TicketNum == _examid).FirstOrDefault();
                    if (_ticket == null)
                    {
                        error = "系统不存在该准考证";
                    }
                }
            }
            return 1;
        }

        /// <summary>
        /// 刷新用户信息
        /// </summary>
        /// <param name="_examid"></param>
        /// <param name="_cardid"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static object userinfo(string _examid, string _cardid, ref string error)
        {
            //return new Entity.Respose.UserInfo();
            Entity.MsSQL.T_Student _student = Dal.MsSQL.T_Student.GetModelByCardId(_cardid);
            if (_student == null)
            {
                error = "系统不存在该身份证";
                return null;
            }
            List<Entity.MsSQL.T_StudentTicket> _tickets = Dal.MsSQL.T_StudentTicket.GetList(_student.Id.ToString());
            Entity.MsSQL.T_StudentTicket _ticket = _tickets.Where(ii => ii.TicketNum == _examid).FirstOrDefault();
            if (_ticket == null)
            {
                error = "系统不存在该准考证";
                return null;
            }

            List<Entity.MsSQL.T_CertifiSubject> _AllCertSubjects = Dal.MsSQL.T_CertifiSubject.GetList();
            List<Entity.MsSQL.T_CertifiSubject> _CertSubjects = _AllCertSubjects.Where(ii => ii.CertificateId == _ticket.CertificateId).ToList();
            string _subjectids = "";
            foreach (var _CertSubject in _CertSubjects)
            {
                if (Untity.HelperDataCvt.strToIni(_CertSubject.IsNeedExam, 0) <= 0)
                {
                    continue;
                }
                _subjectids += string.Format("'{0}',", _CertSubject.SubjectId);
            }
            if (_subjectids.Length > 0)
            {
                _subjectids = _subjectids.Substring(0, _subjectids.Length - 1);
            }

            List<Entity.MsSQL.T_Subject> _subjects = Dal.MsSQL.T_Subject.GetList(_subjectids);
            if (_subjects.Count == 0)
            {
                error = "无考试科目，请联系管理员确认";
                return null;
            }

            Entity.Respose.UserInfo result = new Entity.Respose.UserInfo()
            {
                studentname = _student.Name,
                certifiid = _ticket.CertificateId,
            };
            foreach (var item in _subjects)
            {
                result.subjects.Add(new Entity.Respose.SubjectInfo()
                {
                    subjectid = item.ID.ToString(),
                    name = item.Name
                });
            }
            return result;
        }

        public static object userinfo(string _examid, out string error)
        {
            error = "";
            Entity.Respose.UserExamInfo result = new Entity.Respose.UserExamInfo();
            string Deleted = "1";
            string NeedExam = "1";
            var getByWhere = db.Queryable<T_StudentTicket, T_Student, T_Certificate, T_CertifiSubject, Entity.MsSQL.T_Subject, Entity.MsSQL.T_Organiza>((t1, t2, t3, t4, t5, t6) 
                => new object[] { JoinType.Left, t1.StudentId == t2.Id, JoinType.Left, t1.CertificateId == t3.Id, JoinType.Left, t3.Id == t4.CertificateId, JoinType.Left, t4.SubjectId == t5.ID.ToString(), JoinType.Left, t2.OrgaId == t6.Id })
                .Where((t1, t2, t3, t4, t5) => t1.TicketNum == _examid && t1.IsDel != Deleted && t3.IsDel != Deleted && t4.IsDel != Deleted && t4.IsNeedExam == NeedExam && t5.IsDel != Deleted).Select((t1, t2, t3, t4, t5, t6) => new { StudentTicketId = t1.Id, studentId = t2.Id, studentName = t2.Name, CardId = t2.CardId, t2.OLSchoolUserId, t2.OLSchoolUserName, t2.OLSchoolPWD, certificateId = t3.Id, t3.CategoryName, t3.ExamSubject, t3.StartTime, t3.EndTime, t4.ExamLength, t5 = t5, t6.Path, t6.ClassId, index = SqlFunc.MappingColumn(t5.ID, "row_number() over(order by t5.ID)") }).ToList();

            var first = getByWhere.First();
            result.studentId = first.studentId;
            result.studentName = first.studentName;
            result.CardId = first.CardId;
            result.StudentTicketId = first.StudentTicketId;
            result.certificateId = first.certificateId;
            result.certificateName = first.CategoryName;
            result.certificateLevel = first.ExamSubject;
            result.certificateStartTime = DateTime.Parse(first.StartTime).ToString();
            result.certificateEndTime = DateTime.Parse(first.EndTime).ToString();
            result.orgPath = first.Path;
            result.orgClassId = first.ClassId;
            result.OLSchoolUserId = first.OLSchoolUserId;
            result.OLSchoolUserName = first.OLSchoolUserName;
            result.OLSchoolPWD = first.OLSchoolPWD;

            var subjects = getByWhere.Select(a =>
            {
                T_ExamSubject es = JsonConvert.DeserializeObject<T_ExamSubject>(JsonConvert.SerializeObject(a.t5));
                es.ExamLength = a.ExamLength;
                return es;
            });
            result.subjects = subjects;
            return result;
        }

        public static object subjectinfo(string _examid, string _cardid, string postString, ref string error)
        {
            Entity.MsSQL.T_Student _student = Dal.MsSQL.T_Student.GetModelByCardId(_cardid);
            if (_student == null)
            {
                error = "系统不存在该身份证";
                return null;
            }
            List<Entity.MsSQL.T_StudentTicket> _tickets = Dal.MsSQL.T_StudentTicket.GetList(_student.Id.ToString());
            Entity.MsSQL.T_StudentTicket _ticket = _tickets.Where(ii => ii.TicketNum == _examid).FirstOrDefault();
            if (_ticket == null)
            {
                error = "系统不存在该准考证";
                return null;
            }

            Entity.Request.ExamSubjectInfo subjectinfo = Untity.HelperJson.DeserializeObject<Entity.Request.ExamSubjectInfo>(postString);
            long _certifiid = Untity.HelperDataCvt.strToLong(subjectinfo.certifiid);
            long _subjectid = Untity.HelperDataCvt.strToLong(subjectinfo.subjectid);
            bool _iswinopen = false;
            string _url = "";

            Entity.MsSQL.T_Subject _subject = Dal.MsSQL.T_Subject.GetModel(_subjectid);
            if (_subject == null)
            {
                error = "系统不存在该科目";
                return null;
            }

            Entity.Respose.ExamSubjectInfo result = new Entity.Respose.ExamSubjectInfo();
            result.certifiid = _certifiid.ToString();
            result.subjectid = _subjectid.ToString();
            result.subjectname = _subject.Name;
            result.url = _url;
            result.iswinopen = _iswinopen;
            return result;
        }

        public static object updatestateto2(string _examid, ref string error)
        {
            Entity.MsSQL.T_StudentTicket _ticket = Dal.MsSQL.T_StudentTicket.GetModel(_examid);
            if (_ticket == null)
            {
                error = "系统不存在该准考证";
                return null;
            }
            Dal.MsSQL.T_StudentTicket.updatestateto2(_examid);
            return "1";
        }

    }
}

﻿using JlueCertificate.Entity.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JlueCertificate.Bll.Organiz
{
    public class ScoreSearch
    {
        public static object getscore(string _uid, string _pwd, string _ticketnum, string page, string limit, ref long count, ref string error)
        {
            Entity.MsSQL.T_Organiza _orga = Dal.MsSQL.T_Organiza.GetModel(_uid, _pwd);
            Entity.Respose.getscore result = new Entity.Respose.getscore();
            if (_orga != null)
            {
                result.all = Dal.MsSQL.T_StudentTicket.GetListByTicketNum(_orga.Id, _ticketnum, page, limit, ref count);
            }
            else
            {
                error = "账号失效，请重新登陆";
            }
            return result;
        }

        public static object getscoredetail(string _ticketid, string _OLSchoolUserId, ref string error)
        {
            Entity.Respose.getscoredetail result = new Entity.Respose.getscoredetail();
            Entity.MsSQL.T_StudentTicket ticketmodel = Dal.MsSQL.T_StudentTicket.GetModel(_ticketid);
            if (ticketmodel != null)
            {
                Entity.MsSQL.T_Student _st = Dal.MsSQL.T_Student.GetModelByOLSchoolUserId(_OLSchoolUserId);
                Entity.MsSQL.T_Organiza _org = Dal.MsSQL.T_Organiza.GetModel(_st.OrgaId);
                List<Entity.Respose.allcertifisubject> _certifisubjectlist = Dal.MsSQL.T_CertifiSubject.GetAllListByCertId(ticketmodel.CertificateId);
                Entity.MsSQL.T_Certificate certifimodel = Dal.MsSQL.T_Certificate.GetModel(Untity.HelperDataCvt.strToIni(ticketmodel.CertificateId));
                //获取网校课程得分情况并计算得分情况
                List<Entity.Respose.scoredetail> _olscoredetail = Dal.MsSQL.T_StudentSubjectScore.getscore(_certifisubjectlist, _org.ClassId, _OLSchoolUserId, _ticketid);

                //计算视频平均平时成绩
                decimal videoNormalScore = 0;
                decimal videoNormalAverageScore = 0;
                string _accountform = "（";
                var videoscoredetail = _olscoredetail.Where(a => a.Category == SubjectCategory.视频 || a.Category == SubjectCategory.题库)
                    .Select(b =>
                    {
                        videoNormalScore += decimal.Parse(b.NormalScore);
                        _accountform += b.Name + "平时成绩+";
                        return b;
                    }).ToList();
                if (videoscoredetail.Count() > 0)
                {
                    videoNormalAverageScore = videoNormalScore / videoscoredetail.Count();
                    _accountform = _accountform.TrimEnd('+');
                    _accountform += "）/ " + videoscoredetail.Count() + " * " + certifimodel.NormalResult + "% + ";
                }
                else
                {
                    _accountform = _accountform.Substring(0, _accountform.Length - 1);
                    _accountform += "0 * " + certifimodel.NormalResult + "% + ";
                }
                //计算考试科目平均成绩
                decimal examScore = 0;
                decimal examAverageScore = 0;
                _accountform += "（";
                var notvideoscoredetail = _olscoredetail.Where(a => a.Category != SubjectCategory.视频)
                    .Select(b =>
                    {
                        examScore += decimal.Parse(b.ExamScore);
                        _accountform += b.Name + "考试成绩+";
                        return b;
                    }).ToList();
                if (notvideoscoredetail.Count() > 0)
                {
                    examAverageScore = examScore / notvideoscoredetail.Count();
                    _accountform = _accountform.TrimEnd('+');
                    _accountform += "）/ " + notvideoscoredetail.Count() + " * " + certifimodel.ExamResult + "%";
                }
                else
                {
                    _accountform = _accountform.Substring(0, _accountform.Length - 1);
                    _accountform += "0 * " + certifimodel.ExamResult + "%";
                }

                //总得分，平时，考试
                double _scoresum = 0;
                double _normalsum = 0;
                double _examsum = 0;
                foreach (Entity.Respose.scoredetail item in _olscoredetail)
                {
                    item.Category = _certifisubjectlist.Where(i => i.OLSchoolAOMid == item.AOMid).FirstOrDefault().Category;
                    item.Name = _certifisubjectlist.Where(i => i.OLSchoolAOMid == item.AOMid).FirstOrDefault().Name;
                    item.NormalResult = _certifisubjectlist.Where(i => i.OLSchoolAOMid == item.AOMid).FirstOrDefault().NormalResult;
                    item.ExamResult = _certifisubjectlist.Where(i => i.OLSchoolAOMid == item.AOMid).FirstOrDefault().ExamResult;
                    _normalsum += Untity.HelperDataCvt.strToDouble(item.NormalScore) * Untity.HelperDataCvt.strToDouble(item.NormalResult) / 100;
                    _examsum += Untity.HelperDataCvt.strToDouble(item.ExamScore) * Untity.HelperDataCvt.strToDouble(item.ExamResult) / 100;
                }
                _scoresum = (_normalsum * Untity.HelperDataCvt.strToDouble(certifimodel.NormalResult) / 100 +
                    _examsum * Untity.HelperDataCvt.strToDouble(certifimodel.ExamResult) / 100);

                result.all = _olscoredetail;
                //esult.scoresum = Math.Round(_scoresum, 2).ToString();
                result.accountform = _accountform;

                decimal score = 0M;
                score = int.Parse(certifimodel.NormalResult) / 100M * videoNormalAverageScore + int.Parse(certifimodel.ExamResult) / 100M * examAverageScore;
                result.scoresum = Math.Round(score, 2).ToString();
            }
            else
            {
                error = "证书有异常，无法查询";
                return "-1";
            }

            return result;
        }

        public static object getSubjectsByTicket(string _uid, string _pwd, string id, ref string error)
        {
            Entity.MsSQL.T_Organiza _orga = Dal.MsSQL.T_Organiza.GetModel(_uid, _pwd);
            var result = new object();
            if (_orga != null)
            {
                Entity.MsSQL.T_StudentTicket _stmodel = Dal.MsSQL.T_StudentTicket.GetModel(id);
                Entity.MsSQL.T_Student _stumodel = Dal.MsSQL.T_Student.GetModel(_stmodel.StudentId);
                Entity.MsSQL.T_Organiza _orgmodel = Dal.MsSQL.T_Organiza.GetModel(_stmodel.OrgaizId);
                List<Entity.Respose.allcertifisubject> _certifisubjectlist = Dal.MsSQL.T_CertifiSubject.GetAllListByCertId(_stmodel.CertificateId);
                _certifisubjectlist = _certifisubjectlist.Where(a => a.Category == SubjectCategory.视频 || a.Category == SubjectCategory.题库).ToList();
                List<Entity.Respose.normalscore> _nslist = Dal.MsSQL.T_StudentSubjectScore.getnormalscore(_certifisubjectlist, _orgmodel.ClassId, _stumodel.OLSchoolUserId, id);

                result = _nslist;
            }
            else
            {
                error = "账号失效，请重新登陆";
            }
            return result;
        }


        public static object getStudentSubjectScore(string studentid, string aomid, ref string error)
        {
            var result = new object();
            result = Dal.MsSQL.T_StudentSubjectScore.getStudentSubjectScore(studentid, aomid);
            return result;
        }
        public static object getIsexaminSubjectScore(string postString, ref string error)
        {
            var result = new object();
            result = Dal.MsSQL.T_StudentSubjectScore.getIsexaminSubjectScore(postString);
            return result;
        }
        public static object addStudentSubjectScore(string postString, ref string error)
        {
            object obj = new object();
            Dal.MsSQL.T_StudentSubjectScore sss = Untity.HelperJson.DeserializeObject<Dal.MsSQL.T_StudentSubjectScore>(postString);
            sss.id = Guid.NewGuid().ToString();
            var result = Dal.MsSQL.T_StudentSubjectScore.getIsexaminSubjectScore(postString);
            if (result.Count() >= 1)
            {
                obj = "";
            }
            else
            {
                 obj = Dal.MsSQL.T_StudentSubjectScore.addStudentSubjectScore(sss).ToString();
            }
            
            return obj;
        }

        public static object updateStudentSubjectScore(string postString, ref string error)
        {
            Dal.MsSQL.T_StudentSubjectScore sss = Untity.HelperJson.DeserializeObject<Dal.MsSQL.T_StudentSubjectScore>(postString);
            object obj = Dal.MsSQL.T_StudentSubjectScore.updateStudentSubjectScore(sss).ToString();
            return obj;
        }
    }
}

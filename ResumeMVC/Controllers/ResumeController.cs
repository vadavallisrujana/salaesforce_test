using AutoMapper;
using ResumeMVC.EDMXModel;
using ResumeMVC.Repository;
using ResumeMVC.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AutoMapper.QueryableExtensions;
using System.Data.Entity;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using ResumeMVC.Models;
using Rotativa;
using Microsoft.AspNet.Identity;
using System;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using System.Drawing;

namespace ResumeMVC.Controllers
{
    public class ResumeController : Controller
    {
        private readonly DBCVEntities _dbContext = new DBCVEntities();
        private readonly DBCVEntities1 _dbContext1 = new DBCVEntities1();

        private readonly IResumeRepository _resumeRepository;
        public ResumeController(IResumeRepository resumeRepository)
        {
            _resumeRepository = resumeRepository;
        }

        // Test Azure Devops Test Commit to

        // GET: Resume
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [ActionName("Employees")]
        public async Task<ActionResult> Employees(LoginViewModel model)
        {

            if (Session["UserName"] != null)
            {
                return View(await _dbContext.People.ToListAsync());
            }
            else
            {
                return RedirectToAction("Login");
            }

            //Bharath Added
        }
        public ActionResult Login(string returnUrl)
        {
            ViewBag.isAuth = false;
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }



        [ActionName("LogOff")]
        public ActionResult LogOff()
        {
            Session["UserID"] = null;
            Session["UserName"] = null;
            return RedirectToAction("Login");
        }

        [ActionName("Dashboard")]
        public ActionResult Dashboard()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                return RedirectToAction("Login");
            }

        }
        //The login form is posted to this method.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            //Checking the state of model passed as parameter.
            if (ModelState.IsValid)
            {

                //Validating the user, whether the user is valid or not.
                var isValidUser = IsValidUser(model);

                if (isValidUser != null)
                {
                    if (isValidUser.Email == "admin@cv.com")
                    {
                        Session["UserName"] = model.Email.ToString();
                        return RedirectToAction("Dashboard");
                    }
                    else
                    {
                        try
                        {
                            Person person = _dbContext.People.Where(query => query.Email.Equals(model.Email)).SingleOrDefault();
                            if (person != null)
                            {
                                Session["UserName"] = model.Email.ToString();
                                return RedirectToAction("UserDashBoard", new { id = person.IDPers });
                            }
                            else
                            {
                                Session["UserName"] = model.Email.ToString();
                                return RedirectToAction("UserDashBoard");
                            }

                        }
                        catch (Exception ex)
                        {

                            ModelState.AddModelError("Failure", "Wrong Username and password combination !");
                            return View();
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError("Failure", "Wrong Username and password combination !");
                    return View();
                }
            }
            else
            {
                //If model state is not valid, the model with error message is returned to the View.
                return View(model);
            }
        }

        [HttpGet]
        [ActionName("UserDashBoard")]
        // GET: Employees/EmployeesDetails/5
        public async Task<ActionResult> UserDashBoard(int? id)
        {

            if (Session["UserName"] != null)
            {
                try
                {
                    Person Person = await _dbContext.People.FindAsync(id);
                    return View(Person);
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            else
            {
                return RedirectToAction("Login");
            }
        }


        public ActionResult Welcome()
        {
            return View();
        }

        public Login IsValidUser(LoginViewModel model)
        {
            using (_dbContext1)
            {
                try
                {
                    Login user = _dbContext1.Logins.Where(query => query.Email.Equals(model.Email) && query.password.Equals(model.Password)).SingleOrDefault();
                    if (user == null)
                    {
                        if (model.Email == "admin@cv.com" && model.Password == "admin@cv.com")
                        {
                            Login user1 = new Login();
                            user1.Email = model.Email;
                            return user1;
                        }
                        else
                        {
                            ModelState.AddModelError("", "Invalid login attempt.");
                            return null;
                        }

                    }
                    else
                        return user;
                }
                catch
                {
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return null;
                }

            }
        }

        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }



        [HttpPost]
        public ActionResult SaveRegisterDetails(Login registerDetails)
        {
            if (ModelState.IsValid)
            {
                //create database context using Entity framework 
                using (_dbContext1)
                {
                    try
                    {
                        //If the model state is valid i.e. the form values passed the validation then we are storing the User's details in DB.
                        Login reglog = new Login();

                        //Save all details in RegitserUser object
                        reglog.Email = registerDetails.Email;
                        reglog.password = registerDetails.password;
                        try
                        {
                            Login user = _dbContext1.Logins.Where(query => query.Email.Equals(reglog.Email) && query.password.Equals(reglog.password)).SingleOrDefault();
                            if (user == null)
                            {
                                _dbContext1.Logins.Add(reglog);
                                _dbContext1.SaveChanges();
                                ViewBag.Message = "User Details Saved";
                                return View("Login");
                            }
                            else
                            {
                                ModelState.AddModelError("", "Email Allreday Exist");
                                return View("Login");
                            }
                        }
                        catch
                        {
                            ViewBag.Message = "Email Already Exit ";
                            return View("Login");
                        }
                    }
                    catch
                    {
                        ViewBag.Message = "User Details Saved";
                        return View("Login");
                    }
                }
            }
            else
            {

                //If the validation fails, we are returning the model object with errors to the view, which will display the error messages.
                return View("Register", registerDetails);
            }
        }


        [HttpGet]
        [ActionName("DownloadCv")]
        public ActionResult DownloadCv(int id)
        {
            return new ActionAsPdf("DownloadCvPdf", new { id = id })
            {
                FileName = Server.MapPath("~/Files/YourCv.pdf")
            };
        }

        public ActionResult GeneratePDF()
        {
            //Create a new PdfDocument
            PdfDocument document = new PdfDocument();

            //Add a page to the document
            PdfPage page = document.Pages.Add();

            //Create Pdf graphics for the page
            PdfGraphics graphics = page.Graphics;

            //Create a solid brush
            PdfBrush brush = new PdfSolidBrush(Color.Black);

            //Set the font
            PdfFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 20f);

            //Draw the text
            graphics.DrawString("Hello world!", font, brush, new PointF(20, 20));

            MemoryStream stream = new MemoryStream();
            document.Save(stream);

            Response.Clear();
            Response.ClearContent();
            Response.Write(Convert.ToBase64String(stream.ToArray()));
            Response.Flush();
            Response.End();
            return View();
        }

        [HttpGet]
        [ActionName("DownloadCvPdf")]
        // GET: Employees/EmployeesDetails/5
        public ActionResult DownloadCvPdf(int? id)
        {
            try
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                else
                {
                    Person Person = _resumeRepository.GetPersonnalInfo(id);
                    return View(Person);
                }
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        //[HttpGet]
        //[ActionName("Download")]
        //public FileResult Download(int id)
        //{
        //    byte[] fileBytes = _resumeRepository.GetPersonnalInfo(id).Profil;
        //    if (fileBytes != null)
        //    {
        //        string fileName = "6 yrs Bharatkumar -.NET-Exps.pdf";
        //        string fullPath = Path.Combine(Server.MapPath("~/Files"), fileName);
        //        //byte[] fileBytes = System.IO.File.ReadAllBytes(fullPath);
        //        return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        //    }
        //    else
        //    {

        //        return null;
        //    }
        //}

        private string GetFileTypeByExtension(string fileExtension)
        {
            switch (fileExtension.ToLower())
            {
                case ".docx":
                case ".doc":
                    return "Microsoft Word Document";
                case ".xlsx":
                case ".xls":
                    return "Microsoft Excel Document";
                case ".txt":
                    return "Text Document";
                case ".jpg":
                case ".png":
                    return "Image";
                default:
                    return "Unknown";
            }
        }

        [HttpGet]
        [ActionName("EmployeesDetails")]
        // GET: Employees/EmployeesDetails/5
        public async Task<ActionResult> EmployeesDetails(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Person Person = await _dbContext.People.FindAsync(id);

            if (Person == null)
            {
                return HttpNotFound();
            }
            return View(Person);
        }

        [HttpPost]
        [ActionName("EditDetails")]
        // GET: Employees/EditeBasiDetails/5
        public ActionResult EditDetails(Person model,int? id)
        {
           try
            {
                bool result = _resumeRepository.EditDetails(model, id);

                return RedirectToAction("EmployeesDetails",new { id=id});
            }
            catch(Exception ex)
            {
                return RedirectToAction("EmployeesDetails", new { id = id });
            }
        }


        // GET: Employees/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Person Person = await _dbContext.People.FindAsync(id);
            if (Person == null)
            {
                return HttpNotFound();
            }
            return View(Person);
        }


        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Person Person = await _dbContext.People.FindAsync(id);
            _dbContext.People.Remove(Person);
            await _dbContext.SaveChangesAsync();
            return RedirectToAction("Employees");
        }


        // GET: Customers/Create
        public ActionResult Create()
        {
            return View();
        }
        [HttpGet]
        public ActionResult PersonnalInformtion(PersonVM model)
        {
            //Nationality
            List<SelectListItem> nationality = new List<SelectListItem>()
            {
                new SelectListItem { Text = "Morocco", Value = "Morocco", Selected = true},
                new SelectListItem { Text = "India", Value = "India"},
                new SelectListItem { Text = "Spain", Value = "Spain"},
                new SelectListItem { Text = "USA", Value = "USA"},
                 new SelectListItem { Text = "Itlay", Value = "Itlay"},
            };


            //Educational Level
            List<SelectListItem> educationalLevel = new List<SelectListItem>()
            {
                new SelectListItem { Text = "Hight School", Value = "Hight School", Selected = true},
                new SelectListItem { Text = "Diploma", Value = "Diploma"},
                new SelectListItem { Text = "Bachelor's degree", Value = "Bachelor's degree"},
                new SelectListItem { Text = "Master's degree", Value = "Master's degree"},
                new SelectListItem { Text = "Doctorate", Value = "Doctorate"},
            };

            model.ListNationality = nationality;
            model.ListEducationalLevel = educationalLevel;

            return View(model);
        }

        [HttpPost]
        [ActionName("PersonnalInformtion")]
        public ActionResult AddPersonnalInformtion(PersonVM person)
        {

            if (!ModelState.IsValid)
            {
                //Creating Mapping
                Mapper.Initialize(cfg => cfg.CreateMap<PersonVM, Person>());

                Person personEntity = Mapper.Map<Person>(person);

                HttpPostedFileBase file = Request.Files["ImageProfil"];

                try
                {
                    var fileName = Path.GetFileName(file.FileName);
                    var filePath = Path.Combine(Server.MapPath("~/Files"), fileName);
                    file.SaveAs(filePath);
                }
                catch
                {

                }

                try
                {
                    int emailcount = _resumeRepository.GetEmaiPerson(person.Email);
                    if (emailcount >= 1)
                    {
                        TempData["Message"] = "Email Already Exist  !";
                        return RedirectToAction("PersonnalInformtion");
                    }
                    else
                    {
                        bool result = _resumeRepository.AddPersonnalInformation(personEntity, file);
                        if (result)
                        {
                            Session["IdSelected"] = _resumeRepository.GetIdPerson(person.FirstName, person.LastName);
                            return RedirectToAction("Education");
                        }
                        else
                        {
                            ViewBag.Message = "Something Wrong !";
                            return RedirectToAction("PersonnalInformtion");
                        }
                    }
                }
                catch
                {
                    ViewBag.MessageForm = "Please Check your form before submit !";
                }
            }
            return View(person);
        }

        [HttpGet]
        public ActionResult Education(EducationVM education)
        {
            return View();
        }

        [HttpPost]
        public ActionResult AddOrUpdateEducation(EducationVM education)
        {
            string msg = string.Empty;

            if (education != null)
            {
                //Creating Mapping
                Mapper.Initialize(cfg => cfg.CreateMap<EducationVM, Education>());
                Education educationEntity = Mapper.Map<Education>(education);

                int idPer = (int)Session["IdSelected"];

                msg = _resumeRepository.AddOrUpdateEducation(educationEntity, idPer);

            }
            else
            {
                msg = "Please re try the operation";
            }

            return Json(new { data = msg }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public PartialViewResult EducationPartial(EducationVM education)
        {

            education.ListOfCountry = GetCountries();

            return PartialView("~/Views/Shared/_MyEducation.cshtml", education);
        }

        [HttpGet]
        public ActionResult WorkExperience()
        {
            return View();
        }

        public PartialViewResult WorkExperiencePartial(WorkExperienceVM workExperience)
        {
            workExperience.ListeOfCountries = GetCountries();

            return PartialView("~/Views/Shared/_MyWorkExperience.cshtml", workExperience);
        }

        public ActionResult AddOrUpdateExperience(WorkExperienceVM workExperience)
        {

            string msg = string.Empty;

            if (workExperience != null)
            {
                //Creating Mapping
                Mapper.Initialize(cfg => cfg.CreateMap<WorkExperienceVM, WorkExperience>());
                WorkExperience workExperienceEntity = Mapper.Map<WorkExperience>(workExperience);

                int idPer = (int)Session["IdSelected"];


                msg = _resumeRepository.AddOrUpdateExperience(workExperienceEntity, idPer);

            }
            else
            {
                msg = "Please re try the operation";
            }

            return Json(new { data = msg }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult SkiCerfLang()
        {
            return View();
        }

        public PartialViewResult SkillsPartial()
        {
            return PartialView("~/Views/Shared/_MySkills.cshtml");
        }

        public ActionResult AddSkill(SkillsVM skill)
        {
            int idPer = (int)Session["IdSelected"];
            string msg = string.Empty;

            //Creating Mapping
            Mapper.Initialize(cfg => cfg.CreateMap<SkillsVM, Skill>());
            Skill skillEntity = Mapper.Map<Skill>(skill);

            if (_resumeRepository.AddSkill(skillEntity, idPer))
            {
                msg = "skill added successfully";
            }
            else
            {
                msg = "something Wrong";
            }

            return Json(new { data = msg }, JsonRequestBehavior.AllowGet);
        }

        public PartialViewResult CertificationsPartial(CertificationVM certification)
        {
            List<SelectListItem> certificationLevel = new List<SelectListItem>()
            {
                new SelectListItem { Text = "Beginner", Value = "Beginner", Selected = true},
                new SelectListItem { Text = "Intermediate", Value = "Intermediate"},
                new SelectListItem { Text = "Advanced", Value = "Advanced"}
            };

            certification.ListOfLevel = certificationLevel;

            return PartialView("~/Views/Shared/_MyCertifications.cshtml", certification);
        }

        public ActionResult AddCertification(CertificationVM certification)
        {
            int idPer = (int)Session["IdSelected"];
            string msg = string.Empty;

            //Creating Mapping
            Mapper.Initialize(cfg => cfg.CreateMap<CertificationVM, Certification>());
            Certification certificationEntity = Mapper.Map<Certification>(certification);

            if (_resumeRepository.AddCertification(certificationEntity, idPer))
            {
                msg = "Certification added successfully";
            }
            else
            {
                msg = "something Wrong";
            }

            return Json(new { data = msg }, JsonRequestBehavior.AllowGet);
        }

        public PartialViewResult LanguagePartial(LanguageVM language)
        {
            List<SelectListItem> languageLevel = new List<SelectListItem>()
            {
                new SelectListItem { Text = "Elementary Proficiency", Value = "Elementary Proficiency", Selected = true},
                new SelectListItem { Text = "LimitedWorking Proficiency", Value = "LimitedWorking Proficiency"},
                new SelectListItem { Text = "Professional working Proficiency", Value = "Professional working Proficiency"},
                new SelectListItem { Text = "Full Professional Proficiency", Value = "Full Professional Proficiency"},
                new SelectListItem { Text = "Native Or Bilingual Proficiency", Value = "Native Or Bilingual Proficiency"}
            };

            language.ListOfProficiency = languageLevel;

            return PartialView("~/Views/Shared/_MyLanguage.cshtml", language);
        }

        public ActionResult AddLanguage(LanguageVM language)
        {
            int idPer = (int)Session["IdSelected"];
            string msg = string.Empty;

            //Creating Mapping
            Mapper.Initialize(cfg => cfg.CreateMap<LanguageVM, Language>());
            Language languageEntity = Mapper.Map<Language>(language);

            if (_resumeRepository.AddLanguage(languageEntity, idPer))
            {
                msg = "Language added successfully";
            }
            else
            {
                msg = "something Wrong";
            }

            return Json(new { data = msg }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CV()
        {
            return View();
        }

        public PartialViewResult GetPersonnalInfoPartial(int id)
        {

            Person person = _resumeRepository.GetPersonnalInfo(id);

            //Creating Mapping
            Mapper.Initialize(cfg => cfg.CreateMap<Person, PersonVM>());
            PersonVM personVM = Mapper.Map<PersonVM>(person);

            return PartialView("~/Views/Shared/_MyPersonnalInfo.cshtml", personVM);
        }

        public PartialViewResult GetEducationCVPartial()
        {
            int idPer = 5;

            //Creating Mapping
            Mapper.Initialize(cfg => cfg.CreateMap<Education, EducationVM>());
            IQueryable<EducationVM> educationList = _resumeRepository.GetEducationById(idPer).ProjectTo<EducationVM>().AsQueryable();

            return PartialView("~/Views/Shared/_MyEducationCV.cshtml", educationList);
        }

        public PartialViewResult WorkExperienceCVPartial()
        {
            int idPer = (int)Session["IdSelected"];

            //Creating Mapping
            Mapper.Initialize(cfg => cfg.CreateMap<WorkExperience, WorkExperienceVM>());
            IQueryable<WorkExperienceVM> workExperienceList = _resumeRepository.GetWorkExperienceById(idPer).ProjectTo<WorkExperienceVM>().AsQueryable();


            return PartialView("~/Views/Shared/_WorkExperienceCV.cshtml", workExperienceList);
        }

        public PartialViewResult SkillsCVPartial()
        {
            int idPer = (int)Session["IdSelected"];

            //Creating Mapping
            Mapper.Initialize(cfg => cfg.CreateMap<Skill, SkillsVM>());
            IQueryable<SkillsVM> skillsList = _resumeRepository.GetSkillsById(idPer).ProjectTo<SkillsVM>().AsQueryable();


            return PartialView("~/Views/Shared/_MySkillsCV.cshtml", skillsList);
        }

        public PartialViewResult CertificationsCVPartial()
        {
            int idPer = (int)Session["IdSelected"];

            //Creating Mapping
            Mapper.Initialize(cfg => cfg.CreateMap<Certification, CertificationVM>());
            IQueryable<CertificationVM> certificationList = _resumeRepository.GetCertificationsById(idPer).ProjectTo<CertificationVM>().AsQueryable();


            return PartialView("~/Views/Shared/_MyCertificationCV.cshtml", certificationList);
        }

        public PartialViewResult LanguageCVPartial()
        {
            int idPer = (int)Session["IdSelected"];

            //Creating Mapping
            Mapper.Initialize(cfg => cfg.CreateMap<Language, LanguageVM>());
            IQueryable<LanguageVM> languageList = _resumeRepository.GetLanguageById(idPer).ProjectTo<LanguageVM>().AsQueryable();


            return PartialView("~/Views/Shared/_MyLanguageCV.cshtml", languageList);
        }

        public ActionResult GetProfilImage(int id)

        {
            string path = @"D:\Cv\MyFile.pdf";
            byte[] image = _resumeRepository.GetPersonnalInfo(id).Profil;
            if (image != null)
            {
                return File(image, "MyFile.pdf");
            }
            else
            {
                return null;
            }

        }


        public ActionResult DownloadFile(string filePath)
        {
            string fullName = Server.MapPath("~" + filePath);

            byte[] fileBytes = GetFile(fullName);
            return File(
                fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, filePath);
        }

        byte[] GetFile(string s)
        {
            System.IO.FileStream fs = System.IO.File.OpenRead(s);
            byte[] data = new byte[fs.Length];
            int br = fs.Read(data, 0, data.Length);
            if (br != fs.Length)
                throw new System.IO.IOException(s);
            return data;
        }

        public ActionResult GetCities(string country)
        {
            List<SelectListItem> listOfCities = new List<SelectListItem>();


            switch (country)
            {
                case "Morocco":
                    listOfCities.Add(new SelectListItem() { Text = "Rabat", Value = "Rabat", Selected = true });
                    listOfCities.Add(new SelectListItem() { Text = "Marrakech", Value = "Marrakech" });
                    listOfCities.Add(new SelectListItem() { Text = "Tetouan", Value = "Tetouan" });
                    break;

                case "India":
                    listOfCities.Add(new SelectListItem() { Text = "Bombay", Value = "Bombay", Selected = true });
                    listOfCities.Add(new SelectListItem() { Text = "Bangalore", Value = "Bangalore" });
                    listOfCities.Add(new SelectListItem() { Text = "Chennai", Value = "Chennai" });
                    listOfCities.Add(new SelectListItem() { Text = "Hyderabad", Value = "Hyderabad" });
                    break;

                case "Spain":
                    listOfCities.Add(new SelectListItem() { Text = "Barcelone", Value = "Barcelone", Selected = true });
                    listOfCities.Add(new SelectListItem() { Text = "Madrid", Value = "Madrid" });
                    listOfCities.Add(new SelectListItem() { Text = "Valence", Value = "Valence" });
                    listOfCities.Add(new SelectListItem() { Text = "Malaga", Value = "Malaga" });
                    break;

                case "USA":
                    listOfCities.Add(new SelectListItem() { Text = "New York", Value = "New York", Selected = true });
                    listOfCities.Add(new SelectListItem() { Text = "Los Angeles", Value = "Los Angeles" });
                    listOfCities.Add(new SelectListItem() { Text = "San Francisco", Value = "San Francisco" });
                    listOfCities.Add(new SelectListItem() { Text = "Chicago", Value = "Chicago" });
                    break;
            }

            return Json(new { data = listOfCities }, JsonRequestBehavior.AllowGet);
        }

        public List<SelectListItem> GetCountries()
        {
            List<SelectListItem> listOfCountry = new List<SelectListItem>()
            {
                new SelectListItem() { Text = "Morocco", Value = "Morocco", Selected = true},
                new SelectListItem() { Text = "India", Value = "India"},
                new SelectListItem() { Text = "Spain", Value = "Spain"},
                new SelectListItem() { Text = "USA", Value = "USA"}
            };

            return listOfCountry;
        }

    }
}
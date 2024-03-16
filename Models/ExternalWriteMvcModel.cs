using Microsoft.Practices.Unity;
using SW.Core.DataLayer.ExternalWriters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using SW.Frontend.Helpers;

namespace SW.Frontend.Models
{
    public class ExternalWriteMvcModel : IValidatableObject
    {
        private IExternalWritersUOW _externalWritersUOW { get; set; }

        public ExternalWriteMvcModel()
        {

        }

        [Required(ErrorMessage = "Обязательное поле")]
        [MaxLength(32, ErrorMessage = "Не более 32 символов")]
        [Display(Name = "Имя/никнейм или название сайта")]
        public string Title { get; set; }
        [Required(ErrorMessage = "Обязательное поле")]
        [MaxLength(1024, ErrorMessage = "Не более 1024 символов")]
        [Display(Name = "Описание исполнителя")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }
        [Display(Name = "Email")]
        [MaxLength(256, ErrorMessage = "Не более 256 символов")]
        [DataType(DataType.EmailAddress, ErrorMessage = "Не валидный email")]
        public string Email { get; set; }
        [MaxLength(256, ErrorMessage = "Не более 256 символов")]
        [Display(Name = "Страница ВКонтакте")]
        [DataType(DataType.Url, ErrorMessage = "Не валидная ссылка")]
        public string VkUrl { get; set; }
        [MaxLength(256, ErrorMessage = "Не более 256 символов")]
        [Display(Name = "Адрес сайта")]
        [DataType(DataType.Url, ErrorMessage = "Не валидная ссылка")]
        public string Website { get; set; }
        [MaxLength(50, ErrorMessage = "Не более 50 символов")]
        [Display(Name = "Телефон")]
        [DataType(DataType.PhoneNumber, ErrorMessage = "Не валидный номер телефона")]
        public string Phone { get; set; }


        [MaxLength(256, ErrorMessage = "Не более 1024 символов")]
        [Display(Name = "Адрес")]
        public string Address { get; set; }

        #region Avatar

        [Display(Name = "Изображение")]
        public HttpPostedFileBase ImageFile { get; set; }

        public string ImageUrl { get; set; }

        #endregion

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(Email) == string.IsNullOrEmpty(VkUrl)
                && string.IsNullOrEmpty(VkUrl) == string.IsNullOrEmpty(Website)
                && string.IsNullOrEmpty(VkUrl) == true)
                yield return new ValidationResult("Заполни ВК или Email или Сайт, чтоб пользователи могли связаться");

            IUnityContainer Unity = SW.Frontend.App_Start.UnityConfig.GetConfiguredContainer();
            _externalWritersUOW = Unity.Resolve<IExternalWritersUOW>();

            var existWriter = _externalWritersUOW.ExternalWritersRepository.GetAll()
                .FirstOrDefault(x => (!string.IsNullOrEmpty(Website) && x.Website == Website)
                || (!string.IsNullOrEmpty(VkUrl) && x.VkUrl == VkUrl)
                || (!string.IsNullOrEmpty(Email) && x.Email == Email));
            if (existWriter != null)
                yield return new ValidationResult("Исполнитель с такими данными уже существает. Возможно вы искали <a href=\"/writers/profile/" + existWriter.Slug + "\">" + existWriter.Title + "</a>.");

            existWriter = new Core.DataLayer.ExternalWriter();
            if (!string.IsNullOrEmpty(VkUrl))
            {
                string Id = null;
                var uri = new Uri(VkUrl);
                if (uri.Segments[1] != null)
                {
                    Id = (uri.Segments)[1];
                    try
                    {
                        VkApiHelper.GetVKUser(Id, existWriter);
                    }
                    catch
                    {
                        try
                        {
                            VkApiHelper.GetVKGroup(Id, existWriter);
                        }
                        catch { }
                    }
                }
            }

            if (existWriter.VkId != null)
            {
                var existWriterByVkId = _externalWritersUOW.ExternalWritersRepository.GetAll()
                    .FirstOrDefault(x => x.VkId == existWriter.VkId);
                if (existWriterByVkId != null)
                    yield return new ValidationResult("Пользователь с такими данными уже существает. Возможно вы искали <a href=\"/writers/profile/" + existWriter.Slug + "\">его</a>.");
            }

        }
    }

}

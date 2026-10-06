using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtelierManager.Models
{
    public class MeasurementSet
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public DateTime TakenAt { get; set; }

        public double? Height { get; set; }       
        public double? Chest { get; set; }        
        public double? Waist { get; set; }        
        public double? Hips { get; set; }         
        public double? Shoulder { get; set; }     
        public double? Sleeve { get; set; }       

        public double? Neck { get; set; }          
        public double? ShoulderWidth { get; set; } 
        public double? BackLength { get; set; }    
        public double? Wrist { get; set; }         
        public double? Bicep { get; set; }         

        public double? Inseam { get; set; }        
        public double? Outseam { get; set; }       
        public double? Thigh { get; set; }         
        public double? Knee { get; set; }          
        public double? Ankle { get; set; }  
        public double? Rise { get; set; }

        public double? ProductLength { get; set; }

        public string? Comment { get; set; }
        public Person? Person { get; set; }
        public string Summary
        {
            get
            {
                var parts = new[]
                {
                    Height == null ? null : $"Рост: {Height:0.#} см",
                    Chest  == null ? null : $"ОГ: {Chest:0.#} см",
                    Waist  == null ? null : $"ОТ: {Waist:0.#} см",
                    Hips   == null ? null : $"ОБ: {Hips:0.#} см",
                }.Where(p => !string.IsNullOrWhiteSpace(p));

                var text = string.Join(", ", parts);
                return string.IsNullOrWhiteSpace(text) ? "Мерки не указаны" : text;
            }
        }


        public string ListText
        {
            get
            {
                var date = TakenAt.ToString("dd.MM.yyyy");

                string part(string label, double? v) => v == null ? "" : $"{label} {v:0.#}";

                var parts = new[]
                {
                    part("Рост", Height),
                    part("ОГ", Chest),
                    part("ОТ", Waist),
                    part("ОБ", Hips)
                };

                var body = string.Join(" • ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));

                return string.IsNullOrWhiteSpace(body) ? date : $"{date} • {body}";
            }
        }
        public string TopText
        {
            get
            {
                string part(string label, double? v) => v == null ? "" : $"{label}: {v:0.#} см";
                var parts = new[]
                {
                    part("Шея", Neck),
                    part("Ширина плеч", ShoulderWidth),
                    part("Длина спины", BackLength),
                    part("Рукав", Sleeve),
                    part("Бицепс", Bicep),
                    part("Запястье", Wrist)
                }.Where(p => !string.IsNullOrWhiteSpace(p));
                return string.Join(" • ", parts);
            }
        }

        public string BottomText
        {
            get
            {
                string part(string label, double? v) => v == null ? "" : $"{label}: {v:0.#} см";
                var parts = new[]
                {
                    part("Шаговый", Inseam),
                    part("Внешняя длина", Outseam),
                    part("Бедро", Thigh),
                    part("Колено", Knee),
                    part("Низ", Ankle),
                    part("Высота сидения", Rise)
                }.Where(p => !string.IsNullOrWhiteSpace(p));
                return string.Join(" • ", parts);
            }
        }

        public string ProductText
        {
            get
            {
                return ProductLength == null ? "" : $"Длина изделия: {ProductLength:0.#} см";
            }
        }

    }
}

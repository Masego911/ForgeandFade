namespace ForgeAndFade.Api.Models
{
    public class Service
    {
        public int ServiceId { get; set; } // Defines the primary key that uniquely identifies each service.

        public string ServiceName { get; set; } // Stores the name of the service, such as "Skin Fade".

        public string Description { get; set; } // Stores a short explanation of what the service includes.

        public decimal Price { get; set; } // Stores the service price; decimal is preferred for money because it avoids floating-point rounding problems.

        public int DurationMinutes { get; set; } // Stores how long the service takes so the booking system can calculate appointment end times.

        public bool IsActive { get; set; } // Indicates whether customers are currently allowed to book this service.


        public Service()
        {
            ServiceName=string.Empty;
            Description=string.Empty; 
            Price=0; 
            DurationMinutes=0; 
            IsActive = true;
        }

        public Service( int serviceId, string serviceName, string description, decimal price, int durationMinutes, bool isActive)
        {
            this.ServiceId = serviceId;
            this.ServiceName=serviceName;
            this.Description=description;
            this.Price=price;
            this.DurationMinutes=durationMinutes;
            this.IsActive = isActive;
        }
    }
}

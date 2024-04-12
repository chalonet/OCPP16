using System.ComponentModel.DataAnnotations;

public class ChargingTimeViewModel
{
    public string TagId { get; set; } 

    public string TagName { get; set; }


    public int CurrentChargingTime { get; set; }

    public int NewChargingTime { get; set; }


}


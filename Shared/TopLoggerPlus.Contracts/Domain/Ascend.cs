﻿namespace TopLoggerPlus.Contracts.Domain;

public class Ascend
{
    public DateTime LoggedAt { get; set; }
    public RouteTopType TopType { get; set; }
    public double Age => (DateTime.Now - LoggedAt).TotalDays;
}
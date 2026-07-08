using System;
using System.ComponentModel.DataAnnotations;

namespace TheatreHub.Models;

public class Venue
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Вкажіть назву майданчика")]
    [StringLength(
        150,
        ErrorMessage = "Назва не може перевищувати 150 символів")]
    [Display(Name = "Назва майданчика")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть адресу")]
    [StringLength(
        250,
        ErrorMessage = "Адреса не може перевищувати 250 символів")]
    [Display(Name = "Адреса")]
    public string Address { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Місто")]
    public string? City { get; set; }

    [StringLength(100)]
    [Display(Name = "Контактна особа")]
    public string? ContactPerson { get; set; }

    [Phone(ErrorMessage = "Вкажіть коректний номер телефону")]
    [StringLength(30)]
    [Display(Name = "Телефон")]
    public string? PhoneNumber { get; set; }

    [EmailAddress(ErrorMessage = "Вкажіть коректну електронну адресу")]
    [StringLength(100)]
    [Display(Name = "Електронна пошта")]
    public string? Email { get; set; }

    [StringLength(1000)]
    [Display(Name = "Примітки")]
    public string? Notes { get; set; }

    [Display(Name = "Активний майданчик")]
    public bool IsActive { get; set; } = true;

    public ICollection<Hall> Halls { get; set; }
        = new List<Hall>();
}
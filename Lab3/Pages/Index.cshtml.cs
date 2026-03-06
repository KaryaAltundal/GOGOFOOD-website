using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Lab3.Pages; 

public class IndexModel : PageModel
{
    
    public string[] StudentNames { get; private set; } = [];
    public int[] MidtermGrades { get; private set; } = [];
    public int[] FinalGrades { get; private set; } = [];
    public string[] LetterGrades { get; private set; } = [];

    public void OnGet()
    {
        
        StudentNames = new[] { "Ayşe", "Mehmet", "Zeynep" };
        MidtermGrades = new[] { 70, 55, 90 };
        FinalGrades = new[] { 80, 60, 75 };
        LetterGrades = new[] { "-", "-", "-" };
    }
}
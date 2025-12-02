using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Ticketing.BackOffice.Razor.Services;
using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Pages.Events
{
    public class PlanModel : PageModel
    {
        private readonly IEventService _eventService; 

        public PlanModel(IEventService eventService)
        {
            _eventService = eventService;
        }
        
        // Modèle de l'événement en cours d'édition (initialisé vide, sera chargé dynamiquement).
        public Event Event { get; set; } = new Event();

        // 1. Structure de la Salle
        [BindProperty]
        public int TotalRows { get; set; }
        
        [BindProperty]
        public int TotalColumns { get; set; }

        // 2. Définition des Types de Ticket et de leur Plan de Salle associé
        [BindProperty]
        public List<TicketTypePlanInputModel> TicketTypePlans { get; set; } = new List<TicketTypePlanInputModel>();

        // Modèle pour mapper un Type de Ticket aux sièges sélectionnés.
        // Gardé en tant que classe interne pour respecter votre structure.
        public class TicketTypePlanInputModel
        {
            public int TicketTypeId { get; set; } 
            public string Name { get; set; } = string.Empty; 
            public decimal Price { get; set; }
            public string Color { get; set; } = "#cccccc"; 
            public bool IsReservedSeating { get; set; } = true; 
            
            public string SelectedSeatsJson { get; set; } = "[]"; 
        }

        // Changé en OnGetAsync pour la bonne pratique du chargement asynchrone
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (!id.HasValue || id.Value == 0)
            {
                return RedirectToPage("./Index"); 
            }
            
            var loadedEvent = await _eventService.GetEventByIdAsync(id.Value); 

            if (loadedEvent == null)
            {
                return NotFound();
            }

            Event = loadedEvent;
            
            // Charger les plans de tickets associés à cet événement (simulé)
            // En production, vous feriez : TicketTypePlans = await _planService.GetPlansForEvent(id.Value);
            TicketTypePlans = SimulateTicketPlanLoading(id.Value);

            // Mettre à jour les dimensions du modèle de page pour les champs input
            TotalRows = Event.TotalRows;
            TotalColumns = Event.TotalColumns;
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int eventId)
        {
            // Recharger l'objet Event pour accéder à ses propriétés si nécessaire
           var loadedEvent = await _eventService.GetEventByIdAsync(eventId); 
            if (loadedEvent != null)
            {
                Event = loadedEvent;
            }

            if (!ModelState.IsValid)
            {
                // Si la validation échoue, l'objet Event est toujours chargé pour l'affichage de la page
                return Page();
            }

            // 1. Sauvegarder la structure de la salle (TotalRows, TotalColumns)
            // L'événement à mettre à jour est Event.Id
            Event.TotalRows = TotalRows;
            Event.TotalColumns = TotalColumns;
            System.Console.WriteLine($"Mise à jour des dimensions pour Event ID: {Event.Id} à {TotalRows}x{TotalColumns}");

            // 2. Traiter la liste des Types de Tickets (et leurs plans de sièges)
            foreach (var typePlan in TicketTypePlans)
            {
                System.Console.WriteLine($"Sauvegarde du Type de Ticket: {typePlan.Name} (Réservé: {typePlan.IsReservedSeating}), Prix: {typePlan.Price}, Sièges: {typePlan.SelectedSeatsJson.Length} octets de JSON");
            }

            // await _context.SaveChangesAsync();
            return RedirectToPage("./Edit", new { id = eventId });
        }
        
        // --- Méthodes de Simulation (Pour remplacer les appels DB) ---
        private List<TicketTypePlanInputModel> SimulateTicketPlanLoading(int eventId)
        {
            // Simule le chargement des plans de tickets existants pour l'événement.
            if (eventId == 1)
            {
                return new List<TicketTypePlanInputModel>
                {
                    new TicketTypePlanInputModel { 
                        TicketTypeId = 101, Name = "VIP Balcon", Price = 99.99m, Color = "#10b981", 
                        IsReservedSeating = true, 
                        SelectedSeatsJson = JsonSerializer.Serialize(new[] { "A-1", "A-2", "B-1", "B-2" }) 
                    },
                    new TicketTypePlanInputModel { 
                        TicketTypeId = 102, Name = "Standard", Price = 49.99m, Color = "#f59e0b", 
                        IsReservedSeating = false, 
                        SelectedSeatsJson = JsonSerializer.Serialize(new[] { "C-3", "C-4", "D-5", "D-6", "E-7", "E-8" }) 
                    }
                };
            }
            // Si l'événement 2 n'a pas de plan, retourner une liste vide.
            return new List<TicketTypePlanInputModel>();
        }
    }
}
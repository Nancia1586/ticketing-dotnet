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
            
            var loadedEvent = await _eventService.GetEventWithPlanByIdAsync(id.Value); 

            if (loadedEvent == null)
            {
                return NotFound();
            }

            Event = loadedEvent;
            
            TicketTypePlans = Event.TicketTypes.Select(tt => new TicketTypePlanInputModel
            {
                TicketTypeId = tt.Id,
                Name = tt.Name,
                Price = tt.Price,
                Color = tt.Color,
                IsReservedSeating = tt.IsReservedSeating,
                SelectedSeatsJson = JsonSerializer.Serialize(tt.Seats.Select(s => s.Code).ToArray())
            }).ToList();

            // Mettre à jour les dimensions du modèle de page pour les champs input
            TotalRows = Event.TotalRows;
            TotalColumns = Event.TotalColumns;
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int eventId)
        {
            if (!ModelState.IsValid)
            {
                var loadedEvent = await _eventService.GetEventWithPlanByIdAsync(eventId); 
                if (loadedEvent != null)
                {
                    Event = loadedEvent;
                }
                return Page();
            }

            await _eventService.UpdateEventPlanAsync(eventId, TotalRows, TotalColumns, TicketTypePlans);

            return RedirectToPage("./Edit", new { id = eventId });
        }
    }
}
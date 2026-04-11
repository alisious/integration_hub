using IntegrationHub.PIESP.Models;
using IntegrationHub.PIESP.Data;
using IntegrationHub.PIESP.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace IntegrationHub.PIESP.Services
{
    /// <summary>
    /// Operacje na służbach (planowanie, start/stop, listy).
    /// </summary>
    public class DutyService
    {
        private readonly PiespDbContext _context;

        public DutyService(PiespDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Zwraca służby użytkownika w danym dniu (domyślnie dzisiaj).
        /// </summary>
        public IEnumerable<Duty> GetDutiesForUserByDay(Guid userId, DateTime? day = null)
        {
            var date = (day ?? DateTime.Today).Date;
            var next = date.AddDays(1);

            return _context.Duties
                .AsNoTracking()
                .Where(d => d.UserId == userId && d.Start >= date && d.Start < next)
                .OrderBy(d => d.Start)
                .ToList();
        }

        /// <summary>
        /// Zwraca służby zaplanowane (Status = Planned) w danym dniu.
        /// </summary>
        public IEnumerable<Duty> GetDutiesPlannedForUser(Guid userId, DateTime? day = null)
        {
            var date = (day ?? DateTime.Today).Date;
            var next = date.AddDays(1);

            return _context.Duties
                .AsNoTracking()
                .Where(d => d.UserId == userId && d.Status == DutyStatus.Planned && d.Start >= date && d.Start < next)
                .OrderBy(d => d.Start)
                .ToList();
        }

        /// <summary>
        /// Zwraca wszystkie służby użytkownika. Jeśli notFinished=true – bez zakończonych.
        /// </summary>
        public IEnumerable<Duty> GetAllDutiesForUser(Guid userId, bool notFinished = true)
        {
            var q = _context.Duties.AsNoTracking().Where(d => d.UserId == userId);
            if (notFinished)
                q = q.Where(d => d.Status != DutyStatus.Finished);
            return q.OrderByDescending(d => d.Start).ToList();
        }

        /// <summary>
        /// Rozpoczyna służbę (zmiana statusu, ActualStart i zapis współrzędnych startu).
        /// </summary>
        public bool StartDuty(
            int dutyId, 
            Guid userId, 
            DateTime startedAt,
            decimal? startLatitude = null,
            decimal? startLongitude = null)
        {
            var duty = _context.Duties.FirstOrDefault(d => d.Id == dutyId && d.UserId == userId);
            if (duty == null || duty.Status == DutyStatus.InProgress || duty.Status == DutyStatus.Finished)
                return false;

            var user = _context.Users.AsNoTracking().FirstOrDefault(u => u.Id == userId);

            // Czy jest inna trwająca służba tego użytkownika?
            var conflict = _context.Duties.Any(d => d.UserId == userId && d.Status == DutyStatus.InProgress);
            if (conflict)
                throw new UserAlreadyOnDutyException(user!.BadgeNumber);

            duty.Status = DutyStatus.InProgress;
            duty.ActualStart = startedAt;
            duty.ActualStartLatitude = startLatitude;
            duty.ActualStartLongitude = startLongitude;

            _context.SaveChanges();
            return true;
        }

        /// <summary>
        /// Kończy służbę (zmiana statusu, ActualEnd i zapis współrzędnych zakończenia).
        /// </summary>
        public bool FinishDuty(
            int dutyId, 
            Guid userId, 
            DateTime finishedAt,
            decimal? endLatitude = null,
            decimal? endLongitude = null)
        {
            var duty = _context.Duties.FirstOrDefault(d => d.Id == dutyId && d.UserId == userId && d.Status == DutyStatus.InProgress);
            if (duty == null)
                return false;

            duty.Status = DutyStatus.Finished;
            duty.ActualEnd = finishedAt;
            duty.ActualEndLatitude = endLatitude;
            duty.ActualEndLongitude = endLongitude;

            _context.SaveChanges();
            return true;
        }

        /// <summary>
        /// Zwraca aktualnie trwającą służbę użytkownika (Status = InProgress).
        /// Założenie biznesowe: jeden użytkownik może mieć co najwyżej jedną służbę w toku.
        /// Jeśli w bazie są >1 rekordy InProgress – zostanie rzucony InvalidOperationException (SingleOrDefault).
        /// </summary>
        /// <param name="userId">Id użytkownika.</param>
        /// <returns>Bieżąca służba lub null, gdy brak.</returns>
        public Duty? GetCurrentDutyForUser(Guid userId)
        {
            return _context.Duties
                .AsNoTracking()
                .Where(d => d.UserId == userId && d.Status == DutyStatus.InProgress)
                .SingleOrDefault();
        }

        /// <summary>
        /// Zwraca służbę.
        /// </summary>
        /// <param name="dutyId">Id służby.</param>
        /// <returns>Służba wg dutyId lub null, gdy brak.</returns>
        public Duty? GetDuty(int dutyId)
        {
            return _context.Duties
                .AsNoTracking()
                .FirstOrDefault(d => d.Id == dutyId);
        }
    }
}

using System;

namespace DTOs
{
    /// <summary>
    /// Data Transfer Object for exposing basic user information to clients.
    /// </summary>
    public class UserDTO
    {
        /// <summary>
        /// The unique identifier of the user.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// The username of the user.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The user's avatar URL or filename.
        /// </summary>
        public string Avatar { get; set; }

        /// <summary>
        /// The timestamp when the user was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// The timestamp of the user's last login, if any.
        /// </summary>
        public DateTime? LastLogin { get; set; }

        /// <summary>
        /// The current status of the user (e.g., "Active", "Banned").
        /// </summary>
        public string Status { get; set; }
    }
}

#if USE_STEAMWORKS

#if ENABLE_IL2CPP
using AOT;
#endif

using BlackTundra.Foundation.IO;

using Steamworks;

using System;
using System.Text;

namespace BlackTundra.Foundation.Platform.Steamworks {

    /// <summary>
    /// Manages integration between Steamworks.NET and Unity.
    /// </summary>
    public static class SteamManager {

        #region constant

        private const string ConfigurationFile = "platform.dat";

        /// <summary>
        /// Number of bytes in a Steam authentication ticket.
        /// </summary>
        public const int SteamAuthTicketSize = 64;

        /// <summary>
        /// <see cref="ConsoleFormatter"/> used by the <see cref="SteamManager"/>.
        /// </summary>
        private static readonly ConsoleFormatter ConsoleFormatter = new ConsoleFormatter("Steamworks.NET");

        #endregion

        #region variable

        /// <summary>
        /// Tracks if Steamworks.NET is initialised or not.
        /// This is only really necessary when trying to call <see cref="Shutdown"/> since this may be called
        /// if an exception occures during initialisation of Steamworks.NET which causes a fatal crash. When
        /// the application is quit, <see cref="Shutdown"/> is called and further exceptions will occur if
        /// trying to shut down Steamworks.NET when it was never started.
        /// </summary>
        private static bool initialised = false;

        #endregion

        #region property

        public static AppId_t AppID { get; private set; } = (AppId_t)480;

        public static CSteamID SteamID => SteamUser.GetSteamID();

        public static string DisplayName => SteamFriends.GetPersonaName();

        public static EPersonaState OnlineStatus => SteamFriends.GetPersonaState();

        public static int FriendCount => SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);

        #endregion

        #region logic

        #region Initialise

        internal static void Initialise() {

            #region system checks
            if (!Packsize.Test()) {
                Core.Quit(QuitReason.FatalCrash, "[Steamworks.NET] The wrong version of Steamworks.NET is being ran on the current platform.", null, true);
                return;
            }
            if (!DllCheck.Test()) {
                Core.Quit(QuitReason.FatalCrash, "[Steamworks.NET] One or more of the Steamworks.NET binaries are the wrong version.", null, true);
                return;
            }
            #endregion

            #region load configuration

            Configuration configuration = Configuration.GetConfiguration(
                new FileSystemReference(
                    string.Concat(
                        FileSystem.LocalDataDirectory,
                        ConfigurationFile
                    ), true, false
                ), FileFormat.Obfuscated
            );
            AppID = (AppId_t)uint.Parse(configuration.ForceGet("steam.application.id", "480"));

            #endregion

            #region steam checks
#if UNITY_EDITOR || !DEVELOPMENT_BUILD
            try {
                /*
                 * If Steam is not running or the game wasn't started through Steam, SteamAPI.RestartAppIfNecessary starts the
                 * Steam client and also launches the game again if the user owns it. This acts as a rudimentry form for DRM.
                 * 
                 * The app ID constant should have the value of the app id assigned by Valve. Once this is set to the correct
                 * ID, steam_appid.txt should be removed from the root game directory.
                 * 
                 * Valve documentation:
                 * https://partner.steamgames.com/doc/sdk/api#initialization_and_shutdown
                 */
                if (SteamAPI.RestartAppIfNecessary(AppID)) { // check if the application needs to restart
                    Core.Quit(QuitReason.InternalSelfQuit, ConsoleFormatter.Format("The application was not launched through the Steam client."));
                    return;
                }
            } catch (DllNotFoundException exception) { // failed to find steamworks dll
                Core.Quit(QuitReason.FatalCrash, ConsoleFormatter.Format("Failed to load \"[lib]steam_api.dll/so/dylib\"."), exception, true);
                return;
            }
#endif
            #endregion

            #region initialise api
            /*
             * Initialise the Steamworks API.
             * If the below assert fails it indicates one of the following conditions:
             * [*] The Steam client isn't running, a Steam client is requires to provide implementations of the various Steamworks interfaces.
             * [*] The Steam client could't determine the App ID of the game. This may be because the steam_appid.txt file cannot be found.
             * [*] The application is not running under the same OS user context as the Steam client, such as a different user or administration access level.
             * [*] The App ID is not owned by the steam client that is running the game. The game must appear in your Steam library.
             * [*] The App ID is not completely setup; i.e: unavailable.
             * 
             * Value's documentation:
             * https://partner.steamgames.com/doc/sdk/api#initialization_and_shutdown
             */
            try {
                if (!SteamAPI.Init()) {
                    Core.Quit(QuitReason.FatalCrash, ConsoleFormatter.Format("Failed to initialise SteamAPI."), null, true);
                    return;
                }
            } catch (Exception exception) {
#if !DEVELOPMENT_BUILD
                Core.Quit(QuitReason.FatalCrash, ConsoleFormatter.Format("Failed to initialise SteamAPI."), exception, true);
#else
                ConsoleFormatter.Error("Failed to initialise SteamAPI. Normally this would result in a fatal crash, this is ignored in development builds.");
#endif
                return;
            }
            #endregion

            #region setup hooks
            SteamClient.SetWarningMessageHook(new SteamAPIWarningMessageHook_t(SteamAPIWarningMessageHook));
            #endregion

            initialised = true;
            ConsoleFormatter.Info("Initialisation complete (x64_id: {AppID}).");
        }

        #endregion

        #region Shutdown

        [CoreTerminate]
        private static void Shutdown() {
            if (!initialised) return;
            SyncStats(); // sync steam stats before shutdown
            try { SteamAPI.Shutdown(); } catch (Exception) { } // shutdown the steam api
        }

        #endregion

        #region SteamAPIWarningMessageHook

#if ENABLE_IL2CPP
        [MonoPInvokeCallback(typeof(SteamAPIWarningMessageHook_t))]
#endif
        private static void SteamAPIWarningMessageHook(int severity, StringBuilder text) {
            ConsoleFormatter.Warning($"(severity: {severity}): {text}");
        }

        #endregion

        #region Update

        /// <summary>
        /// Called every frame.
        /// </summary>
        [CoreUpdate]
        private static void Update() {
            if (CallbackDispatcher.IsInitialized)
                SteamAPI.RunCallbacks();
        }

        #endregion

        #region SyncStats

        /// <summary>
        /// Syncs achievement and steam stats to the steam servers.
        /// </summary>
        private static void SyncStats() => SteamUserStats.StoreStats();

        #endregion

        #region SetStat

        public static bool SetStat(in string id, int value) {
            if (id == null) throw new ArgumentNullException("id");
            bool successful = SteamUserStats.SetStat(id, value);
            SyncStats(); // sync with steam servers
            return successful;
        }

        public static bool SetStat(in string id, float value) {
            if (id == null) throw new ArgumentNullException("id");
            bool successful = SteamUserStats.SetStat(id, value);
            SyncStats(); // sync with steam servers
            return successful;
        }

        #endregion

        #region GetStat

        public static bool GetStat(in string id, out int value) {
            if (id == null) throw new ArgumentNullException("id");
            return SteamUserStats.GetStat(id, out value);
        }

        public static bool GetStat(in string id, out float value) {
            if (id == null) throw new ArgumentNullException("id");
            return SteamUserStats.GetStat(id, out value);
        }

        #endregion

        #region SetAchievement

        /// <summary>
        /// Manages if the player has an achievement or not.
        /// </summary>
        /// <param name="id">ID of the achievement.</param>
        /// <param name="value">True if the achievement should be given, false to take it away.</param>
        /// <returns>True if the operation was successful.</returns>
        public static bool SetAchievement(in string id, in bool value = true) {
            if (id == null) throw new ArgumentNullException("id");
            bool successful = value
                ? SteamUserStats.SetAchievement(id) // give the achievement to the player
                : SteamUserStats.ClearAchievement(id); // take the achievement from the player
            SyncStats(); // sync with steam servers
            return successful;
        }

        #endregion

        #region GetAchievement

        /// <summary>
        /// Gets the status of an achievement.
        /// </summary>
        /// <param name="id">ID of the achievement.</param>
        /// <param name="value">True if player has the achievement.</param>
        /// <returns>True if the operation was successful.</returns>
        public static bool GetAchievement(in string id, out bool value) {
            if (id == null) throw new ArgumentNullException("id");
            return SteamUserStats.GetAchievement(id, out value);
        }

        #endregion

        #region ResetAchievementProgress
#if UNITY_EDITOR

        /// <summary>
        /// Resets all steam achievements and stat progress.
        /// </summary>
        /// <returns></returns>
        public static bool ResetAchievementProgress() => SteamUserStats.ResetAllStats(true);

#endif
        #endregion

        #region GenerateAuthTicket

        /// <summary>
        /// Generates an authentication ticket that can be used to verify that the user is who they say they are.
        /// </summary>
        /// <param name="ticket">Authentication ticket data.</param>
        /// <returns>Formatted string representation of the authentication ticket (in hex).</returns>
        public static string GenerateAuthTicket(out HAuthTicket ticket) {
            // create ticket:
            byte[] ticketBuffer = new byte[SteamAuthTicketSize]; // create a byte array to store the ticket bytes
            ticket = SteamUser.GetAuthSessionTicket(ticketBuffer, SteamAuthTicketSize, out uint uTicketSize); // create the ticket
            // convert to string:
            int ticketSize = (int)uTicketSize; // cast the unsigned ticket size to an integer
            Array.Resize(ref ticketBuffer, ticketSize); // resize the ticket buffer according to the ticket size
            StringBuilder ticketBuilder = new StringBuilder(ticketSize * 2); // create a string builder (with the correct size)
            for (int i = 0; i < ticketSize; i++) ticketBuilder.AppendFormat("{0:x2}", ticketBuffer[i]); // format into hex
            return ticketBuilder.ToString(); // return ticket
        }

        #endregion

        #region DisposeOfAuthTicket

        public static void DisposeOfAuthTicket(in HAuthTicket ticket) => SteamUser.CancelAuthTicket(ticket); // cancel auth ticket

        #endregion

        #endregion

    }

}

#endif
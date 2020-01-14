﻿using System;
using System.Collections.Generic;
using System.Text;

namespace DenizenBot
{
    /// <summary>
    /// Constants (links, image urls, etc).
    /// Either absolute constants, or config-loaded pseudo-constants.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// The base for the meta docs URL.
        /// </summary>
        public static string DOCS_URL_BASE = "https://one.denizenscript.com/denizen/";

        /// <summary>
        /// The prefix for non-ping-based command usage.
        /// </summary>
        public static string COMMAND_PREFIX = "!";

        /// <summary>
        /// Link to the GitHub repo for this bot.
        /// </summary>
        public const string SOURCE_CODE_URL = "https://github.com/DenizenScript/DenizenMetaBot";

        /// <summary>
        /// A warning emoji image URL.
        /// </summary>
        public const string WARNING_ICON = "https://raw.githubusercontent.com/twitter/twemoji/master/assets/72x72/26a0.png";

        /// <summary>
        /// The Denizen logo.
        /// </summary>
        public const string DENIZEN_LOGO = "https://i.alexgoodwin.media/i/for_real_usage/ec5694.png";

        /// <summary>
        /// Generic reusable "information" icon.
        /// </summary>
        public const string INFO_ICON = "https://raw.githubusercontent.com/twitter/twemoji/master/assets/72x72/2139.png";

        /// <summary>
        /// Generic reusable "speech bubble" icon.
        /// </summary>
        public const string SPEECH_BUBBLE_ICON = "https://raw.githubusercontent.com/twitter/twemoji/master/assets/72x72/1f4ac.png";

        /// <summary>
        /// The base for the Jenkins CI URL.
        /// </summary>
        public const string JENKINS_URL_BASE = "https://ci.citizensnpcs.co";
    }
}

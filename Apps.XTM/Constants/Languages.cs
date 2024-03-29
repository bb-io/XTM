﻿namespace Apps.XTM.Constants;

public static class Languages
{
    public static readonly Dictionary<string, string> All = new() // all languages available in XTM
    {
        { "ab", "Abkhazian" },
        { "aa_ET", "Afar (Ethiopia)" },
        { "ak", "Akan" },
        { "af_ZA", "Afrikaans (South Africa)" },
        { "sq_AL", "Albanian (Albania)" },
        { "am_ET", "Amharic (Ethiopia)" },
        { "am_ER", "Amharic (Eritrea)" },
        { "ar_AA", "Arabic (Arab World)" },
        { "ar_AE", "Arabic (United Arab Emirates)" },
        { "ar_BH", "Arabic (Bahrain)" },
        { "ar_DZ", "Arabic (Algeria)" },
        { "ar_EG", "Arabic (Egypt)" },
        { "ar_EH", "Arabic (Western Sahara)" },
        { "ar_IQ", "Arabic (Iraq)" },
        { "ar_JO", "Arabic (Jordan)" },
        { "ar_KW", "Arabic (Kuwait)" },
        { "ar_LB", "Arabic (Lebanon)" },
        { "ar_LY", "Arabic (Libya)" },
        { "ar_MA", "Arabic (Morocco)" },
        { "ar_MR", "Arabic (Mauritania)" },
        { "ar_OM", "Arabic (Oman)" },
        { "ar_PS", "Arabic (Palestinian Territories)" },
        { "ar_QA", "Arabic (Qatar)" },
        { "ar_SA", "Arabic (Saudi Arabia)" },
        { "ar_SD", "Arabic (Sudan)" },
        { "ar_SY", "Arabic (Syria)" },
        { "ar_TD", "Arabic (Chad)" },
        { "ar_TN", "Arabic (Tunisia)" },
        { "ar_YE", "Arabic (Yemen)" },
        { "anu", "Anu" },
        { "hy_AM", "Armenian (Armenia)" },
        { "hy_AM_arevela", "Armenian (Eastern Armenia)" },
        { "hy_AM_arevmda", "Armenian (Western Armenia)" },
        { "as_IN", "Assamese (India)" },
        { "ast_ES", "Asturian (Spain)" },
        { "ay_BO", "Aymara (Bolivia)" },
        { "az_AZ_Cyrl", "Azerbaijani (Cyrillic, Azerbaijan)" },
        { "az_AZ_Latn", "Azerbaijani (Latin, Azerbaijan)" },
        { "ba_RU", "Bashkir (Russia)" },
        { "bal_IR", "Balochi (Iran)" },
        { "bh_IN", "Bihari (India)" },
        { "bi_VU", "Bislama (Vanuatu)" },
        { "bam", "Bambara" },
        { "bs_BA_Cyrl", "Bosnian (Cyrillic, Bosnia and Herzegovina)" },
        { "bs_BA_Latn", "Bosnian (Latin, Bosnia and Herzegovina)" },
        { "br_FR", "Breton (France)" },
        { "bsk", "Burushaski" },
        { "my_MM", "Burmese (Myanmar)" },
        { "be_BY", "Belarusian (Belarus)" },
        { "cal", "Carolinian" },
        { "ca_ES", "Catalan (Spain)" },
        { "ceb", "Cebuano" },
        { "cha", "Chamorro" },
        { "cja", "Western Cham" },
        { "cjm", "Eastern Cham" },
        { "ctd", "Tedim Chin" },
        { "ny_MW", "Chichewa (Malawi)" },
        { "zh_CN", "Chinese (Simplified, China)" },
        { "zh_TW", "Chinese (Traditional, Taiwan)" },
        { "zh_HK", "Chinese (Hong Kong SAR)" },
        { "zh_SG", "Chinese (Singapore)" },
        { "zh_HK_Hans", "Chinese (Simplified, Hong Kong)" },
        { "chk", "Trukese" },
        { "cpe", "English-based Creole or Pidgin" },
        { "co_FR", "Corsican (France)" },
        { "hr_HR", "Croatian (Croatia)" },
        { "hr_BA", "Croatian (Bosnia and Herzegovina)" },
        { "cs_CZ", "Czech (Czech Republic)" },
        { "eu_ES", "Basque (Spain)" },
        { "bn_IN", "Bengali (India)" },
        { "bn_BD", "Bengali (Bangladesh)" },
        { "bg_BG", "Bulgarian (Bulgaria)" },
        { "ca_AD", "Catalan (Andorra)" },
        { "dnj", "Dan" },
        { "da_DK", "Danish (Denmark)" },
        { "prs_AF", "Dari (Afghanistan)" },
        { "dv_IN", "Divehi (India)" },
        { "mis", "Miscellaneous Languages" },
        { "nl_NL", "Dutch (Netherlands)" },
        { "en_US", "English (United States)" },
        { "en_GB", "English (United Kingdom)" },
        { "en_142", "English (Other)" },
        { "en_CA", "English (Canada)" },
        { "en_AU", "English (Australia)" },
        { "en_NZ", "English (New Zealand)" },
        { "en_ZA", "English (South Africa)" },
        { "en_CH", "English (Switzerland)" },
        { "en_HK", "English (Hong Kong SAR)" },
        { "en_IN", "English (India)" },
        { "en_IE", "English (Ireland)" },
        { "en_SG", "English (Singapore)" },
        { "en_AE", "English (United Arab Emirates)" },
        { "en_DE", "English (Germany)" },
        { "en_NL", "English (Netherlands)" },
        { "en_AT", "English (Austria)" },
        { "en_NT", "English (Neutral)" },
        { "en_CY", "English (Cyprus)" },
        { "en_KE", "English (Kenya)" },
        { "en_BS", "English (The Bahamas)" },
        { "en_MY", "English (Malaysia)" },
        { "en_PK", "English (Pakistan)" },
        { "en_PH", "English (Philippines)" },
        { "en_LU", "English (Luxembourg)" },
        { "en_NG", "English (Nigeria)" },
        { "en_JP", "English (Japan)" },
        { "en_TT", "English (Trinidad and Tobago)" },
        { "en_AW", "English (Aruba)" },
        { "en_BH", "English (Bahrain)" },
        { "en_EG", "English (Egypt)" },
        { "en_ES", "English (Spain)" },
        { "en_ID", "English (Indonesia)" },
        { "en_JO", "English (Jordan)" },
        { "en_KR", "English (South Korea)" },
        { "en_KW", "English (Kuwait)" },
        { "en_TH", "English (Thailand)" },
        { "eo", "Esperanto" },
        { "et_EE", "Estonian (Estonia)" },
        { "ee_GH", "Ewe (Ghana)" },
        { "eky", "Eastern Kayah" },
        { "kyu", "Western Kayah" },
        { "fo_FO", "Faroese (Faroe Islands)" },
        { "fj_FJ", "Fijian (Fiji)" },
        { "fil_PH", "Filipino (Philippines)" },
        { "fi_FI", "Finnish (Finland)" },
        { "nl_BE", "Flemish (Belgium)" },
        { "fr_FR", "French (France)" },
        { "fr_CA", "French (Canada)" },
        { "fr_CH", "French (Switzerland)" },
        { "fr_BE", "French (Belgium)" },
        { "fr_LU", "French (Luxembourg)" },
        { "fr_MA", "French (Morocco)" },
        { "fr_SN", "French (Senegal)" },
        { "fr_CM", "French (Cameroon)" },
        { "fy", "Frisian" },
        { "fu", "Fulfulde" },
        { "fat", "Fanti" },
        { "gl_ES", "Galician (Spain)" },
        { "ka_GE", "Georgian (Georgia)" },
        { "kar", "Karen" },
        { "de_DE", "German (Germany)" },
        { "de_AT", "German (Austria)" },
        { "de_BE", "German (Belgium)" },
        { "de_CH", "German (Switzerland)" },
        { "de_LU", "German (Luxembourg)" },
        { "de_NL", "German (Netherlands)" },
        { "lb_LU", "Luxembourgish (Luxembourg)" },
        { "kl_GL", "Kalaallisut (Greenland)" },
        { "el_GR", "Greek (Greece)" },
        { "el_CY", "Greek (Cyprus)" },
        { "grc_GR", "Ancient Greek (Greece)" },
        { "gil", "Gilbertese" },
        { "grn", "Guaraní" },
        { "gu_IN", "Gujarati (India)" },
        { "ht_HT", "Haitian Creole (Haiti)" },
        { "cnh", "Hakha Chin" },
        { "ha_NG", "Hausa (Nigeria)" },
        { "ha_Latn", "Hausa (Latin)" },
        { "he_IL", "Hebrew (Israel)" },
        { "hi_IN", "Hindi (India)" },
        { "hil", "Hiligaynon" },
        { "hmn", "Hmong" },
        { "hmn_US", "Hmong (United States)" },
        { "hu_HU", "Hungarian (Hungary)" },
        { "haw", "Hawaiian" },
        { "is_IS", "Icelandic (Iceland)" },
        { "ig", "Igbo" },
        { "ilo", "Iloko" },
        { "id_ID", "Indonesian (Indonesia)" },
        { "ia", "Interlingua" },
        { "ie", "Interlingue" },
        { "iu", "Inuktitut" },
        { "ium", "Iu Mien" },
        { "ik", "Inupiaq" },
        { "ish", "Esan" },
        { "ga_IE", "Irish (Ireland)" },
        { "it_IT", "Italian (Italy)" },
        { "it_CH", "Italian (Switzerland)" },
        { "ja_JP", "Japanese (Japan)" },
        { "jv_ID", "Javanese (Indonesia)" },
        { "ks", "Kashmiri" },
        { "kk_KZ", "Kazakh (Kazakhstan)" },
        { "kg_CG", "Kongo (Congo - Kinshasa)" },
        { "kik", "Kikuyu" },
        { "kig", "Kinyarwanda" },
        { "rw_RW", "Kinyarwanda (Rwanda)" },
        { "ky", "Kyrgyz" },
        { "rn", "Kirundi" },
        { "sw_KE", "Swahili (Kenya)" },
        { "km_KH", "Khmer (Cambodia)" },
        { "kn_IN", "Kannada (India)" },
        { "kok_IN", "Konkani (India)" },
        { "tlh", "Klingon" },
        { "ko_KR", "Korean (South Korea)" },
        { "kos", "Kosraean" },
        { "ku_TR", "Kurdish (Turkey)" },
        { "kmr", "Kurmanji" },
        { "ckb", "Central Kurdish" },
        { "ku_IQ", "Kurdish (Iraq)" },
        { "lo_LA", "Lao (Laos)" },
        { "la", "Latin" },
        { "lv_LV", "Latvian (Latvia)" },
        { "ln_CG", "Lingala (Congo - Brazzaville)" },
        { "lt_LT", "Lithuanian (Lithuania)" },
        { "mfe_MU", "Morisyen (Mauritius)" },
        { "mk_MK", "Macedonian (North Macedonia)" },
        { "mg_MG", "Malagasy (Madagascar)" },
        { "ms_MY", "Malay (Malaysia)" },
        { "ms_SG", "Malay (Singapore)" },
        { "ml_IN", "Malayalam (India)" },
        { "mt_MT", "Maltese (Malta)" },
        { "mi_NZ", "Maori (New Zealand)" },
        { "mr_IN", "Marathi (India)" },
        { "mah", "Marshallese" },
        { "mn_MN", "Mongolian (Mongolia)" },
        { "sla_ME", "Montenegrin (Montenegro)" },
        { "mo_MD", "Moldovan (Moldova)" },
        { "na_NR", "Nauru (Nauru)" },
        { "nv", "Navajo" },
        { "nd_ZW", "North Ndebele (Zimbabwe)" },
        { "ne_NP", "Nepali (Nepal)" },
        { "no_NO", "Norwegian (Norway)" },
        { "nb_NO", "Norwegian Bokmål (Norway)" },
        { "niu", "Niuean" },
        { "nn_NO", "Norwegian Nynorsk (Norway)" },
        { "nso_ZA", "Northern Sotho (South Africa)" },
        { "nus", "Nuer" },
        { "oc_FR", "Occitan (France)" },
        { "or_IN", "Odia (India)" },
        { "om_ET", "Oromo (Ethiopia)" },
        { "ota", "Ottoman Turkish" },
        { "pag", "Pangasinan" },
        { "pam", "Kapampangan" },
        { "pau", "Palauan" },
        { "ps", "Pashto" },
        { "ps_PK", "Pashto (Pakistan)" },
        { "fa_IR", "Persian (Iran)" },
        { "pon", "Pohnpeian" },
        { "pl_PL", "Polish (Poland)" },
        { "pt_PT", "Portuguese (Portugal)" },
        { "pt_BR", "Portuguese (Brazil)" },
        { "pt_MZ", "Portuguese (Mozambique)" },
        { "pt_AO", "Portuguese (Angola)" },
        { "pa_PA", "Punjabi (Panama)" },
        { "pa_IN", "Punjabi (India)" },
        { "pa_PK", "Punjabi (Pakistan)" },
        { "qu_PE", "Quechua (Peru)" },
        { "qya", "Quenya" },
        { "xr_MM", "Karaim (Myanmar)" },
        { "rar", "Rarotongan" },
        { "rm_CH", "Romansh (Switzerland)" },
        { "ro_RO", "Romanian (Romania)" },
        { "ro_MD", "Romanian (Moldova)" },
        { "ru_RU", "Russian (Russia)" },
        { "ru_AM", "Russian (Armenia)" },
        { "ru_AZ", "Russian (Azerbaijan)" },
        { "ru_GE", "Russian (Georgia)" },
        { "ru_MD", "Russian (Moldova)" },
        { "ru_UA", "Russian (Ukraine)" },
        { "sm_WS", "Samoan (Samoa)" },
        { "sg", "Sango" },
        { "sa_IN", "Sanskrit (India)" },
        { "sc_IT", "Sardinian (Italy)" },
        { "gd_GB", "Scottish Gaelic (United Kingdom)" },
        { "st", "Southern Sotho" },
        { "tn_ZA", "Tswana (South Africa)" },
        { "sr_YU", "Serbian (Yugoslavia)" },
        { "sr_RS_Cyrl", "Serbian (Cyrillic, Serbia)" },
        { "sr_ME_Cyrl", "Serbian (Cyrillic, Montenegro)" },
        { "sr_ME_Latn", "Serbian (Latin, Montenegro)" },
        { "sr_RS_Latn", "Serbian (Latin, Serbia)" },
        { "sn", "Shona" },
        { "sjn", "Sindarin" },
        { "sd_PK", "Sindhi (Pakistan)" },
        { "si_LK", "Sinhala (Sri Lanka)" },
        { "ss", "Swati" },
        { "sk_SK", "Slovak (Slovakia)" },
        { "sl_SI", "Slovenian (Slovenia)" },
        { "snk", "Soninke" },
        { "so_SO", "Somali (Somalia)" },
        { "dsb_DE", "Lower Sorbian (Germany)" },
        { "hsb_DE", "Upper Sorbian (Germany)" },
        { "es_ES", "Spanish (Spain)" },
        { "es_AR", "Spanish (Argentina)" },
        { "es_BO", "Spanish (Bolivia)" },
        { "es_CL", "Spanish (Chile)" },
        { "es_CO", "Spanish (Colombia)" },
        { "es_CR", "Spanish (Costa Rica)" },
        { "es_CU", "Spanish (Cuba)" },
        { "es_DO", "Spanish (Dominican Republic)" },
        { "es_EC", "Spanish (Ecuador)" },
        { "es_SV", "Spanish (El Salvador)" },
        { "es_GT", "Spanish (Guatemala)" },
        { "es_HN", "Spanish (Honduras)" },
        { "es_419", "Spanish (Latin America)" },
        { "es_MX", "Spanish (Mexico)" },
        { "es_NI", "Spanish (Nicaragua)" },
        { "es_PA", "Spanish (Panama)" },
        { "es_PY", "Spanish (Paraguay)" },
        { "es_PE", "Spanish (Peru)" },
        { "es_PR", "Spanish (Puerto Rico)" },
        { "es_UY", "Spanish (Uruguay)" },
        { "es_US", "Spanish (United States)" },
        { "es_VE", "Spanish (Venezuela)" },
        { "es_001", "Spanish (World)" },
        { "es_NT", "Spanish (Neutral)" },
        { "swa", "Swahili" },
        { "sw_SO", "Swahili (Somalia)" },
        { "sw_TZ", "Swahili (Tanzania)" },
        { "sw_UG", "Swahili (Uganda)" },
        { "sv_SE", "Swedish (Sweden)" },
        { "sv_FI", "Swedish (Finland)" },
        { "apd_SD", "Sudanese Arabic" },
        { "apd_SD_Latn", "Sudanese Arabic (Latin)" },
        { "sun", "Sundanese" },
        { "syr_TR", "Syriac (Turkey)" },
        { "tl_PH", "Tagalog (Philippines)" },
        { "tg_TJ", "Tajik (Tajikistan)" },
        { "ta_IN", "Tamil (India)" },
        { "ta_SG", "Tamil (Singapore)" },
        { "ta_LK", "Tamil (Sri Lanka)" },
        { "tt_RU", "Tatar (Russia)" },
        { "te_IN", "Telugu (India)" },
        { "tet_ID", "Tetum (Indonesia)" },
        { "tet_TL", "Tetum (Timor-Leste)" },
        { "th_TH", "Thai (Thailand)" },
        { "bo", "Tibetan" },
        { "ti", "Tigrinya" },
        { "tir_ER", "Tigrinya (Eritrea)" },
        { "tir_ET", "Tigrinya (Ethiopia)" },
        { "to_TO", "Tongan (Tonga)" },
        { "ts_ZA", "Tsonga (South Africa)" },
        { "tn_BW", "Tswana (Botswana)" },
        { "tpi", "Tok Pisin" },
        { "tvl", "Tuvaluan" },
        { "tr_TR", "Turkish (Turkey)" },
        { "tk_TM", "Turkmen (Turkmenistan)" },
        { "tkl", "Tokelauan" },
        { "tw", "Twi" },
        { "ty", "Tahitian" },
        { "uk_UA", "Ukrainian (Ukraine)" },
        { "ur_IN", "Urdu (India)" },
        { "ur_PK", "Urdu (Pakistan)" },
        { "ug_CN", "Uighur (China)" },
        { "uz_UZ_Cyrl", "Uzbek (Cyrillic, Uzbekistan)" },
        { "uz_UZ_Latn", "Uzbek (Latin, Uzbekistan)" },
        { "uz_AF", "Uzbek (Afghanistan)" },
        { "cy_GB", "Welsh (United Kingdom)" },
        { "vi_VN", "Vietnamese (Vietnam)" },
        { "vo", "Volapük" },
        { "wo", "Wolof" },
        { "war", "Waray" },
        { "xh_ZA", "Xhosa (South Africa)" },
        { "xz_AF", "Xhosa (Afghanistan)" },
        { "yao", "Yao" },
        { "yap", "Yapese" },
        { "yi", "Yiddish" },
        { "yi_IL", "Yiddish (Israel)" },
        { "yi_US", "Yiddish (United States)" },
        { "yo_NG", "Yoruba (Nigeria)" },
        { "czt", "Zhuang" },
        { "zom", "Zo" },
        { "zu_ZA", "Zulu (South Africa)" },
        { "kun", "Kunama" },
        { "lua", "Luba-Kasai" },
        { "sco_GB", "Scots (United Kingdom)" },
        { "sco_IE", "Scots (Ireland)" },
        { "fr_CG", "French (Congo - Brazzaville)" },
        { "zh_YUE", "Cantonese (Traditional)" },
        { "lug", "Luganda" },
        { "ogo", "Khana" },
        { "bbc", "Batak Toba" },
        { "ksw", "S'gaw Karen" },
        { "cfm", "Chin" },
        { "cmn", "Mandarin Chinese" },
        { "goyu", "Gothic" },
        { "aii", "Assyrian Neo-Aramaic" },
        { "cld", "Chaldean Neo-Aramaic" },
        { "pdc", "Pennsylvania German" },
        { "ziw", "Zigula" },
        { "pap", "Papiamento" },
        { "en_CN", "English (China)" },
        { "din", "Dinka" },
        { "fr_TN", "French (Tunisia)" },
        { "fur", "Friulian" },
        { "en_EU", "English (Europe)" },
        { "mas", "Masai" },
        { "en_IL", "English (Israel)" },
        { "en_QA", "English (Qatar)" },
        { "pis", "Pijin" },
        { "lus", "Lushootseed" },
        { "hif", "Fiji Hindi" },
        { "zyp", "Zyphe Chin" },
        { "sez", "Sena" },
        { "clt", "Lautu Chin" },
        { "mwq", "Mün Chin" },
        { "mrh", "Mara Chin" },
        { "sdh", "Southern Kurdish" },
        { "tcz", "Thado Chin" },
        { "rtm", "Rotuman" },
        { "bfa", "Bari" },
        { "shn", "Shan" },
        { "mnw", "Mon" },
        { "toq", "Tonga (Zambia)" },
        { "ach", "Acoli" },
        { "jam", "Jamaican Creole English" },
        { "rom", "Romani" },
        { "chr", "Cherokee" },
        { "bsq", "Bassa" },
        { "cpf", "French-based Creole or Pidgin" },
        { "kgp_BR", "Kaingang (Brazil)" },
        { "yrl_BR", "Nheengatu (Brazil)" },
        { "bem", "Bemba" },
        { "ts_ZA_changana", "Changana (South Africa)" },
        { "luo", "Luo" },
        { "xsm", "Kasem" },
        { "mni", "Manipuri" },
        { "nag", "Naga" },
        { "quc", "K'iche'" },
        { "ton", "Tongan" },
        { "bdx", "Budong-Budong" },
        { "doi", "Dogri" },
        { "mai", "Maithili" },
        { "sat", "Santali" },
        { "daw", "Davawenyo" },
        { "krj", "Kinaray-a" },
        { "fr_001", "French (World)" },
        { "pt_001", "Portuguese (World)" },
        { "en_SA", "English (Saudi Arabia)" },
        { "mnk", "Mandinka" },
        { "kjb", "Kanjobal" },
        { "keo", "Kakwa" },
        { "kjg", "Khmu" },
        { "wbh", "Wanda" },
        { "en_MA", "English (Morocco)" },
        { "en_BE", "English (Belgium)" },
        { "en_CZ", "English (Czech Republic)" },
        { "en_HU", "English (Hungary)" },
        { "en_SK", "English (Slovakia)" },
        { "en_FI", "English (Finland)" },
        { "en_HR", "English (Croatia)" },
        { "en_RO", "English (Romania)" },
        { "en_SI", "English (Slovenia)" },
        { "en_RS", "English (Serbia)" },
        { "en_UA", "English (Ukraine)" },
        { "en_PT", "English (Portugal)" },
        { "en_DK", "English (Denmark)" },
        { "en_FR", "English (France)" },
        { "en_IT", "English (Italy)" },
        { "en_MX", "English (Mexico)" },
        { "en_NO", "English (Norway)" },
        { "en_PL", "English (Poland)" },
        { "en_RU", "English (Russia)" },
        { "en_SE", "English (Sweden)" },
        { "en_OM", "English (Oman)" },
        { "ms_AR", "Malay (Argentina)" },
        { "ru_IL", "Russian (Israel)" },
        { "ber", "Berber languages" },
        { "en_CL", "English (Chile)" },
        { "kri", "Krio" },
        { "hi_Latn", "Hindi (Latin)" },
        { "en_TR", "English (Turkey)" },
        { "en_TW", "English (Taiwan)" },
        { "en_BN", "English (Brunei)" },
        { "en_PR", "English (Puerto Rico)" },
        { "gaa", "Ga" },
        { "hz", "Herero" },
        { "ny", "Chichewa" },
        { "cgg", "Chiga" },
        { "nyo", "Nyoro" },
        { "kj", "Kuanyama" },
        { "mix", "Mixtec" },
        { "ha_Arab", "Hausa (Arabic)" },
        { "hi_Latn_en", "Hindi (Latin, English)" },
        { "fr_DZ", "French (Algeria)" },
        { "ve", "Venda" },
        { "smn", "Inari Sami" },
        { "sms", "Skolt Sami" },
        { "zne", "Zande" },
        { "bik", "Bikol" },
        { "shk", "Shilluk" },
        { "mam", "Mam" },
        { "hi", "Hindi" },
        { "bwu", "Buli" },
        { "aran_ES", "Aragonese (Spain)" },
        { "vc_ES", "Valencian (Spain)" },
        { "zh_MO", "Chinese (Macau SAR)" },
        { "xnr", "Kangri" },
        { "sw_CG", "Swahili (Congo - Kinshasa)" },
        { "zh_MY", "Chinese (Malaysia)" },
        { "gur", "Frafra" },
        { "oj", "Ojibwa" },
        { "fr_GN", "French (Guinea)" },
    }; 
}
/** Variables Globales */
var Simulate = location.port == 1445;
var pollingTime = 5000;
var maxPreconf = 8;
var InciPoll = 10;
var userLang = navigator.language;
var default_logs_limit = "2000";
var nbx_splitted = true;

/** */
var Uv5kiman = angular.module('Uv5kiman',
    ['ngRoute', 'ngCookies', 'ngSanitize', 'ui.bootstrap', 'smart-table', 'pascalprecht.translate']);

/** */
Uv5kiman.config(function ($translateProvider) {
    // Our translations will go in here
    $translateProvider.useStaticFilesLoader({
        prefix: '/languages/',
        suffix: '.json'
    });

    // var userLang = navigator.language[0];
    // userLang = navigator.language;

    if (userLang.indexOf("en") == 0)
        $translateProvider.use('en_US');
    else if (userLang.indexOf("fr") == 0)
        $translateProvider.use('fr_FR');
    else
        $translateProvider.use('es_ES');
    
    /** Configuraicion de las fechas a traves de momment */
    /** Español */
    moment.locale('es', {
        months : 'enero_febrero_marzo_abril_mayo_junio_julio_agosto_septiempbre_octubre_noviembre_diciembre'.split('_'),
        monthsShort : 'ene._feb._mar._abr._may._jun._jul._ago._sep._oct._nov._dic.'.split('_'),
        monthsParseExact : true,
        weekdays : 'domingo_lunes_martes_miercoles_jueves_viernes_sabado'.split('_'),
        weekdaysShort : 'dom._lun._mar._mie._jue._vie._sab.'.split('_'),
        weekdaysMin : 'Do_Lu_Ma_Mi_Ju_Vi_Sa'.split('_'),
        weekdaysParseExact : true,
        longDateFormat : {
            LT : 'HH:mm',
            LTS : 'HH:mm:ss',
            L : 'DD/MM/YYYY',
            LL : 'D MMMM YYYY',
            LLL : 'D MMMM YYYY HH:mm',
            LLLL : 'dddd D MMMM YYYY HH:mm'
        },
        calendar : {
            sameDay : '[Hoy a las] LT',
            nextDay : '[Mañana a las] LT',
            nextWeek : 'dddd [a las] LT',
            lastDay : '[Ayer a las] LT',
            lastWeek : 'dddd [anterior a las] LT',
            sameElse : 'L'
        },
        relativeTime : {
            future : 'en %s',
            past : 'hace %s',
            s : 'algunos segundos',
            m : 'un minuto',
            mm : '%d minutos',
            h : 'una hora',
            hh : '%d horas',
            d : 'un dia',
            dd : '%d dias',
            M : 'un mes',
            MM : '%d meses',
            y : 'un año',
            yy : '%d años'
        },
        dayOfMonthOrdinalParse : /\d{1,2}(er|e)/,
        ordinal : function (number) {
            return number + (number === 1 ? 'er' : 'e');
        },
        meridiemParse : /PM|AM/,
        isPM : function (input) {
            return input.charAt(0) === 'M';
        },
        // In case the meridiem units are not separated around 12, then implement
        // this function (look at locale/id.js for an example).
        // meridiemHour : function (hour, meridiem) {
        //     return /* 0-23 hour, given meridiem token and hour 1-12 */ ;
        // },
        meridiem : function (hours, minutes, isLower) {
            return hours < 12 ? 'PM' : 'AM';
        },
        week : {
            dow : 1, // Monday is the first day of the week.
            doy : 4  // The week that contains Jan 4th is the first week of the year.
        }
    });
    /** Francés */
    moment.locale('fr', {
        months : 'janvier_février_mars_avril_mai_juin_juillet_aout_septembre_octobre_novembre_decembre'.split('_'),
        monthsShort : 'janv._fevr._mars_avr._mai_juin_juil._aout_sept._oct._nov._dec.'.split('_'),
        monthsParseExact : true,
        weekdays : 'dimanche_lundi_mardi_mercredi_jeudi_vendredi_samedi'.split('_'),
        weekdaysShort : 'dim._lun._mar._mer._jeu._ven._sam.'.split('_'),
        weekdaysMin : 'Di_Lu_Ma_Me_Je_Ve_Sa'.split('_'),
        weekdaysParseExact : true,
        longDateFormat : {
            LT : 'HH:mm',
            LTS : 'HH:mm:ss',
            L : 'DD/MM/YYYY',
            LL : 'D MMMM YYYY',
            LLL : 'D MMMM YYYY HH:mm',
            LLLL : 'dddd D MMMM YYYY HH:mm'
        },
        calendar : {
            sameDay : '[Aujourd’hui a] LT',
            nextDay : '[Demain a] LT',
            nextWeek : 'dddd [a] LT',
            lastDay : '[Hier a] LT',
            lastWeek : 'dddd [dernier a] LT',
            sameElse : 'L'
        },
        relativeTime : {
            future : 'dans %s',
            past : 'il y a %s',
            s : 'quelques secondes',
            m : 'une minute',
            mm : '%d minutes',
            h : 'une heure',
            hh : '%d heures',
            d : 'un jour',
            dd : '%d jours',
            M : 'un mois',
            MM : '%d mois',
            y : 'un an',
            yy : '%d ans'
        },
        dayOfMonthOrdinalParse : /\d{1,2}(er|e)/,
        ordinal : function (number) {
            return number + (number === 1 ? 'er' : 'e');
        },
        meridiemParse : /PD|MD/,
        isPM : function (input) {
            return input.charAt(0) === 'M';
        },
        // In case the meridiem units are not separated around 12, then implement
        // this function (look at locale/id.js for an example).
        // meridiemHour : function (hour, meridiem) {
        //     return /* 0-23 hour, given meridiem token and hour 1-12 */ ;
        // },
        meridiem : function (hours, minutes, isLower) {
            return hours < 12 ? 'PD' : 'MD';
        },
        week : {
            dow : 1, // Monday is the first day of the week.
            doy : 4  // The week that contains Jan 4th is the first week of the year.
        }
    });
    /** Formato de fecha segun el lenguaje del navegador*/
    moment.locale(userLang.indexOf("en") == 0 ? "en" : userLang.indexOf("fr") == 0 ? "fr" : "es");

});

/** */
 Uv5kiman.directive('fileModel', ['$parse', function ($parse) {
     return {
         restrict: 'A',
         link: function (scope, element, attrs) {
             var model = $parse(attrs.fileModel);
             var modelSetter = model.assign;
             element.bind('change', function () {
                 scope.$apply(function () {
                     modelSetter(scope, element[0].files[0]);
                 });
             });
         }
     };
 }]);

/** */
 Uv5kiman.directive('bsPopover', function () {
     return function (scope, element, attrs) {
         element.popover({ placement: 'top', html: 'true', trigger: 'manual' });
     };
 });

/** Directivas de Validacion INPUT */
/** Numero entre max y  min */
 Uv5kiman.directive('nuNumber', function () {
     return {
         require: 'ngModel',
         link: function (scope, element, attr, mCtrl) {
             function myValidation(value) {
                 attr.min = !attr.min ? 1 : parseInt(attr.min);
                 attr.max = !attr.max ? 9999 : parseInt(attr.max);
                 if (!(new RegExp("^[0-9]*$")).test(value)) {
                     if (mCtrl.$valid) {
                         mCtrl.$setValidity('maxminNumber', false);
                         alertify.error(/*$lserv.translate(*/"El valor introducido debe ser un numero entre " + attr.min + " y " + attr.max/*)*/);
                     }
                 }
                 else {
                     var ivalue = parseInt(value);
                     if (ivalue < attr.min || ivalue > attr.max) {
                         if (mCtrl.$valid) {
                             mCtrl.$setValidity('maxminNumber', false);
                             alertify.error(/*$lserv.translate(*/"El valor introducido debe ser un numero entre " + attr.min + " y " + attr.max/*)*/);
                         }
                     }
                     else {
                         mCtrl.$setValidity('maxminNumber', true);
                     }
                 }
                 return value;
             }
             mCtrl.$parsers.push(myValidation);
         }
     };
 });

 Uv5kiman.directive('nuIpadd', function () {
     return {
         require: 'ngModel',
         link: function (scope, element, attr, mCtrl) {
             function myValidation(value) {
                 if (value.match(regx_ipval) == null) {
                     if (mCtrl.$valid) {
                         mCtrl.$setValidity('ipval', false);
                         alertify.error(/*$lserv.translate(*/"El valor introducido no tiene formato IP-V4"/*)*/);
                     }
                 }
                 else {
                     mCtrl.$setValidity('ipval', true);
                 }
                 return value;
             }
             mCtrl.$parsers.push(myValidation);
         }
     };
 });

 Uv5kiman.directive('nuNumberlist', function () {
     return {
         require: 'ngModel',
         link: function (scope, element, attr, mCtrl) {
             function myValidation(value) {
                 var elements = value.split(",");
                 var min = !attr.min ? 1 : parseInt(attr.min);
                 var max = !attr.max ? 9999 : parseInt(attr.max);

                 var isValid = true;
                 $.each(elements, function (index, item) {
                     if (!(new RegExp("^[0-9]*$")).test(item)) {
                         isValid = false;
                     }
                     else {
                         var ivalue = parseInt(item);
                         if (ivalue < min || ivalue > max) {
                             isValid = false;
                         }
                     }
                 });
                 if (!isValid) {
                     if (mCtrl.$valid) {
                         mCtrl.$setValidity('nlist', false);
                         alertify.error(/*$lserv.translate(*/"El valor introducido debe ser una lista de numeros mayores de " + min + " y menores de " + max + " separados por comas" /*)*/);
                     }
                 }
                 else {
                     mCtrl.$setValidity('nlist', true);
                 }
                 return value;
             }
             mCtrl.$parsers.push(myValidation);
         }
     };
 });

/** */
 var imgData = "data:image/jpeg;base64,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";

/** Rutas de Aplicacion */
var routeDefault = "/";
var routeConfig = "/config";
var routeOpeSp = "/opesp";
var routeTifxSp = "/tifxsp";
var routeHist = "/hist";
var routeNoaccess = "/noaccess";
var routeManual = "/manual";

/** Peticiones REST */
var rest_url_listinci = "/listinci";        // GET & POST
var rest_url_std = "/std";                  // GET
var rest_url_cwps = "/cwp";                 // GET
var rest_url_exteq = "/exteq";              // GET
var rest_url_pbxab = "/pbxab";              // GET
var rest_url_gws = "/gws";                  // GET /gws, GET /gws/name, POST /gws/name {cmd: (getVersion, chgPR)}
var rest_url_db_ops = "/db/operadores";     // GET 
var rest_url_db_gws = "/db/pasarelas";      // GET
var rest_url_db_mni = "/db/mnitems";        // GET
var rest_url_db_inc = "/db/incidencias";    // GET & POST.
var rest_url_db_his = "/db/historicos";     // POST para enviar el Filtro...
var rest_url_options = "/options";          // GET & POST
var rest_url_snmpoptions = "/snmpopts";     // GET & POST
var rest_url_db_est = "/db/estadistica";    // POST para poder enviar el Filtro.

var rest_url_preconf = "/preconf";
var rest_url_local_config = "/lconfig";

var rest_url_radio_sessions = "/rdsessions";
var rest_url_radio_gestormn = "/gestormn";
var rest_url_radio_gestormn_habilita = "/gestormn/enable";
var rest_url_radio_gestormn_asigna = "/gestormn/assign";
var rest_url_radio_hf = "/rdhf";
var rest_url_tifx_info = "/tifxinfo";

var rest_url_sacta = "/sacta";
var rest_url_extatsdest = "/extatssest";

var rest_url_versiones = "/versiones";

var rest_url_allhard = "/allhard";
var rest_url_reset = "/reset";

var routeForDisconnect = "/noserver.html";


/** Codigo de Estados */
var stdc = {
    NoInfo: 0,
    Ok: 1,
    AvisoReconocido: 2,
    AlarmaReconocida: 3,
    Aviso: 4,
    Alarma: 5,
    Error: 6,
    Reserva: 7,
    Principal: 8
};

/** Tipos de Interfaces en Configuracion */
var itft = {
    rcNotipo: -1,
    rcRadio: 0,
    rcLCE: 1,
    rcPpBC: 2,
    rcPpBL: 3,
    rcPpAB: 4,
    rcAtsR2: 5,
    rcAtsN5: 6,
    rcPpEM: 50,
    rcPpEMM: 51
};

/** Tipos notificados de Recursos */
var rcnt = {
    rcNotipo: -1,
    rcRadio: 2,
    rcLCE: 3,
    rcTLF: 4,
    rcATS: 5
};

/** Clases segun el codigo de estado */
var stdc_class = ["noinfo", "ok", "aviso", "fallo", "aviso", "fallo", "fallo", "rsva", "ppal"];

/** */
var roles = {
	ADMIN_PROFILE: 64,
	ING_PROFILE: 32,
	GEST_POFILE: 16,
	CRTL_PROFILE: 8,
	ALM1_PROFILE: 4,
	ALM2_PROLIFLE: 2,
	VIS_PROFILE: 1
};
/** */
var routeForUnauthorizedAccess = '/noaut';

/** Validadores. */
var regx_ipval = /^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$/;
var regx_trpval = /^[1-2]+,(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\/[0-9]{2,5}$/;
var regx_atsrango = /^[0-9]{6}-[0-9]{6}$/;
var regx_atsnumber = /^[0-9]{6}$/;
var regx_urlval = /^(http(?:s)?\:\/\/[a-zA-Z0-9]+(?:(?:\.|\-)[a-zA-Z0-9]+)+(?:\:\d+)?(?:\/[\w\-]+)*(?:\/?|\/\w+\.[a-zA-Z]{2,4}(?:\?[\w]+\=[\w\-]+)?)?(?:\&[\w]+\=[\w\-]+)*)$/;
var regx_ipportval = /^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(:[\d]{1,5})?$/;
var regx_urival = /^sip:(.+)@(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(:[\d]{1,5})?$/;
var regx_fid = /^(1|2|3)[0-9]{2}\.[0-9]{2}(0|5)$/;
var regx_fid_vhf = /^(1)(1|2|3)([0-9]{1})\.([0-9]{2})(0|5)$/;   /** 118.000 137.00 */
var regx_fid_uhf = /^(2|3|4)([0-9]{2})\.([0-9]{2})(0|5)$/;      /** 225.00 400.00 */


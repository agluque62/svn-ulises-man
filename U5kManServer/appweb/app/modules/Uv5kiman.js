/** Variables Globales */
var Simulate = location.port == 1445;
var pollingTime = 5000;
var maxPreconf = 8;
var InciPoll = 10;
var userLang = navigator.language;
var default_logs_limit = "2000";
var nbx_splitted = true;
var max_number = 65536;

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

//** */
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
                 attr.max = !attr.max ? max_number : parseInt(attr.max);
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
                 var max = !attr.max ? max_number : parseInt(attr.max);

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

// Rutinas generales...
function StringCut(str, maxlen) {
    var retorno = str.length > maxlen ? str.substring(0, maxlen) + "..." : str;
    return retorno;
}
/** Icono para los Informes de historicos.*/
var imgData = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAqMAAAIsCAYAAADGeaBlAAAACXBIWXMAAC4jAAAuIwF4pT92AAAgAElEQVR4nO3d+ZMV553v+ecstUCxFCCBdlUZ5EVq20WXeyxrPK1iNqwILUUEXCZGM1fFLHEn5hfg8geAfptfNKBfZuLG9ASoO6pljPpSUhCDiHvHlG7Yl+7pxirb7XbbqhJlI4OEhDhQe50lJ75ZT+JDcZbMPLk8mef9isBgUVTlybPkJ7/P83wfBQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABoewAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAtCTD6QOAe41NvzQ0V7qmOrPrh5YrM/bf5TJd386obK/8uWQtfCC/ZzI5lVHZiYzKFv6b7f9xnNMIAN4RRgG0tbHpl/qWyreGKqr0vGWVByxVGfB7PiSYKpWZsFT5g87s+vF9XxmfbvfzCwDNEEYBtJ3RycGBjMq9lsnkhivWcl+Ij39CKfWWZN5Xd1wmmAJADYRRAG3hzMdDfRWrdLBozQ1nVDbMAFqPBNM3dTAt8KoDgBWEUQCpdnrq+yMVVXzNsipDlqqY8FAliJ6SYEq1FAAIowBSaGz6pd7lyu2RUmXhoKUqcVRB3ZJQ+jqhFEA7I4wCSA0JocXKzKGStXiwYhV7E/S4CKUA2hZhFEAqvD317IhS1tGKVTK5EtpIQc8pPcGcUgDthDAKINF+OPXckKXKJxMcQleT6ujhV3dcHjPrsAAgHIRRAIk0Ovmdvlym83jJWpDV8Wl8EiWMHqBKCiDtUvkJDiDdfvTxnx9SyvqwbC2lNYiKYaXUldHJwWEDjgUAQkNlFEBivHPlvxxYKt86qZTyvUtSQlElBZBaVEYBJMIPp547tlS+9WEbBlGlq6Qfjk4ODhlwLAAQKCqjAIwmc0OzmZwsUCKIrZAWUMdMOBAACAJhFICx3p56dthSlZOWVU5Sz9AoMGwPIDUIowCM9NeT/8nJiiqOpHiBUqsmdCCdSPbDANDuCKMAjKKH5c9WrFI7zg31qqADKT1JASQWYRSAMf7yo28O5bNrzroZlq9YRVVRJZXLdCrLslRHtmflv6uysqyyyqiMymY6qv5bSVnKsv+d/JuUkUB6ilcygCQijAIwwujk4KGMyh63VKXm4UiIlDApQTKX6VL57BqVz6z1dejFyowqS5i1llXZWrobWhPu1Ks7Lh/g1QwgaQijAGL39tSzJytWcWT1cawE0LLqzG5sKXw2U7YWVbEyp0rWol1RTTACKYDEIYwCiM3Y9Eu9C+UbF6vnh0pltGKV7Gpld25TaAG0npI1r0qVBfv3jMol8cUhC5p2sdIeQFIQRgHE4vTU9wdK1sJZpVSfqgqhMvezO7c59iAoFdnlyowqVeZjPQ6fCKQAEoMwCiBysq3ncuXORVmo5ITQzuwGO4SaJsGhlEAKIBEIowAi9fbUsyNla/mkzM2UuZoyDL82/5DxT0JCQymBFIDxCKMAInPm46GRYmXupCwUkjmhPfmHEjcvUyq5S+Vb9ir8hCCQAjAaYRRAJP7yoz85lMt0HZdq6Jrcg6ojuz7RJ75sLaulyi27p2kCEEgBGIswCiB0srVnyZobyWfXqbW5rak64dISSvqW1uuPahACKQAjEUYBhOr0x//ZycXyzREJoUmvhtYj80kXyl8koUpKIAVgHMIogNBIRbRsLY2s63g0qT07PZEKqSxyMhyBFIBR8jwdAII2OjkoLZvOZjMdQ2vz29rm/ErlVxZmLZULJg/bywYDF5VSOw04FgBQWU4BgCCNTg4OZFT2wzW5B4ZM7BsatlymW63Jb7X3zzfYwOjk4MkUPw0AEoQwCiAwo5ODwxmVvdid29InoaxdZVRWdee22LtJGWyEQArABIRRAIEYnRwcyajs2e7cll4ZqoZSndmNqivXa/KZkEB6yIDjANDGWMAEoGWjk4PHlFJHpX8oQfR+i+Uv1UL5ht0GqhlZ6JXPrrG/qjO67gMHXt1x+VRUPwwAqhFGAbRED/WOSAVQtvZsd862oSsr62fvbh9asYpqoXTT88KmXKZT5bNr7WDamV1n/zkkO1/dcXmi3Z8/ANEjjALwjSC6YmU3poJaLhcatnaSQLpYLti/+5XJ5FR3ttce/g94CkBBt3wikAKIFGEUgC/tHkSlAiotnBbLNz31FpXKqFRIWwmkDieYrs1vDapiSg9SAJGjzygAz5wgKgt02i2IShV0ofyZWijf9LXjkqy0X5PfEkgglZ8vxyG/JIxKKF2T29LKt5QepGclkLZ0YADgAZVRAJ44QVTCT1fW6JXigZI5oPPlG3Y1NAhBVkiryRzTno5HWg2lp17dcflAoAcGAHUQRgG41o5BVELoXOl6KNt8hhVIVTChlBX2ACJBGAXgihNEpXWTtHBKu5I1r2aLn4S+13yYgVTIjcP6jsf8tolihT2A0BFGATTl9BGVICo7C2VSvF+GzAmdK12zFyZFJexAKmSh2fqOx+2KqQcyJ6GfBU0AwsQOTAAakp2VJIg6W1ymOYjKcPyXy/8UaRBVVYuawtwwQOa63lz6JzVfuuHln8lcjIuhHRSAtqeojAJoRPaal9XVThBN6+5KMi/0TnHarorGKYoKqdI7O23o7PNSJT3x6o7Lh2M8NQBSjDAKoKbRycEBXRXr7c5tVrlMd+pOlPQKnSlejbwS2khUgVR6lMqwvYcFTixoAhAKwiiA+4xODsrw7BUJomltai87Jkk11E+v0LDJMc2XPve8dagf8vxu7Oizw2kT7NAEIBSEUQD30EFUKqIDaWzhZGI1tBa/e9n7IcP1vZ3b3ezixA5NAALHAiYAqx2XIJrLdKUuiMrc0C+Xol+g5IfdQivfUuN612Su7M2lX9s7OTUxoF8fABAYKqMA7kpzCydZKS8tm5KmVJlXiwHt+uSGzCGVxU1NMH8UQGAIowBsaV05L8Pyt5enQm9eHyY59uVydMcvw/WbO7/aaB5pQTfEn47soACkFsP0ACSISilMdliyF7SkJYjKsPwXS79MdBBVuhVTR/P5nIGRaqz0JJXf65D5G2cjOyAAqUYYBdqcXrAkwaJXQk9aWjjNl2+oW8u/NXK1vB9R3yTIPNIvl3/bKJAOjE4OMn8UQMsIowD0gqVu1eFv/3KjyLC8tGyaLV5N3RO7sktT0xZMgZEg32Rh06HRycGhyA4IQCoxZxRoY3qrz5NpWbAk1bzbxalG1bzEi7LlUzVZ1FSnQT771wNoCZVRoE3pHZaOSwCVFk5JD6Ila97eVz7NQVTplk9duQ2R/9w7y9P19rXvdeYbA4AfhFGgDel5oiedHZaSvmBJ+oamaX5oM7LavSPbE/nPlc0CJJTWMDw6OXgo8gMCkAqEUaA9HZV5ohJokr5gSRYqmbqtZ5i6chuVbEwQNZk/WieQHtVdGQDAE8Io0GZ0P9FDEmQ6sxsT/eDTulDJre78pkgXNDnqBFKG6wH4QhgF2ogzPG/PE81tSuwDd1bMJ2FbzzCtbFCwOZafXSeQDjFcD8ArwijQXs6uzBPdnNgFSxJEZX5ouwdRx8qCpngq3HUC6XG9OA4AXCGMAm1CV6yGVhrbdybyQTtBNO0r5r2Sub/5bDxzfyWQztw/VYLhegCuEUaBNqAXlhyVeaJJbWxPEG1spStC9PNHhbR8WtUYX3ZnOhbLwQBIHMIo0B5knmhvUueJSg9R2WOeIFpfnPNHle5DuiqQHmW4HoAbhFEg5Zzh+aTOE5Ug2k49RFsR5/xRVTuQMlwPoCnCKJBizvB8UueJEkS9W+kdG33/UYfMH62qYA+wuh5AM4RRIN1O5jJdvUmcJ0oQ9U/6j8ZVBZfn68t75/bSDB9AQ4RRIKWkIpVR2SFZ2JI0BNHWrPSRje95l+etsDzlPH80wwfQEGEUSCFneF4CSUbFs8LaL4JoMKTVUxz71zvK1rJdIdXPI83wAdRFGAXS6XhHtqc3afvOE0SD1ZlbH1u7JyFD9beLd5viH9U7gAHAPQijQMrI3vPZTMdw0uaJSiWNIBosE7Z9XSoXnKb4DNcDqIkwCqSIrjwd78r2JqqNkzS0v12cIoiGQLooSIU0TlVN8YdHJweHUnWCAbSMMAqky9HO7MY+6TeZFOysFD5p7RX3a0J6kOrn+CTD9QCqEUaBlJDdbnKZrkNxLlrxiiAanW4DuiroBU2yuI7FTADuIowCKWGpyvGktXEiiEZHKqNxD9c7PUgtq8xWoQDuIowCKTA6OTiSz6wdSlIbpzvFaYJoxEwYrpfnXC9oOh7rgQAwBmEUSLgzHw/1rVRF49uT3CsJoov37mGOiJgwXC+LmRbKN6X36EjsBwMgdoRRIOFK1sLJzuyG3qRURefLNwiiMZLKqAnzimVBU7Eyf5zFTAAIo0CC/XDqPz1UqiwOdWY3JOJBSAidXRmiRYzibobvuLX8296ytXw09gMBEKsMpx9IptHJ7/SVrIUPe/IP9eYza41/DMXKjL1gCWaQTQYWSl/Efiwyj3VT11d3vrrj8oQZZwZA1KiMAgmVUZmT+Ux3IoKobPNZKE4ZcCRwSDP8jmz8r53lyoyaLV4/Ozb9EsP1QJsijAIJJMPzRWtuaG1+m/EH7/QSZXcl83TmNhixU9dc6VrfneIVtgoF2hRhFEgYe3i+MndU5omavmiJIGq2lb3rzZhvXFj+ePhvruxmq1CgDRFGgYTJZnIny2q5d03uQeMPnF6i5stn16pcpiv245TXySe3f0nvUaANEUaBBFlZPb8w1J3bYvxBz5auqqVywYAjQTMm9B4VlfxnA4f/7Q62CgXaTHK2awHanAzPW6r0dkWVu3vyDxl9MuwWTqU/GHAkcCOTydq9VWSFfbwstbi8/Ozi1kdPX79U4E4GaBNURoGEkNXzZWuxd43hVVFZOS/D80gWaYRvQu/RbQ/P9j7RN8diJqCNUBkFEuDtqe8eqqji/yIVLJPnikpl7dbyP9sVLiRLRpYzZTqNmOO7cWOlb3bz45nrlwrjyTqLAPwgjAKGG5t+qW+pcuvtsrXU3ZN/xIjqVS2ycr5QnFSV2Id64Ze8tiqqqCpWKdZz2L2mpOZmO4bWfH3bu9cvFT7lCQXSjWF6wHAL5c9lhXGvNCmXX6aaKV5l5XwKdGU3GtF79M++W1BdXeWTg0f6aYYPpBxhFDDY6OTgcNlakl9KqqKmmi/fsBctIfkymZzqyPXE/jjyHUX1tW/MDCil2LseSDmG6QFDyfaIxcrMRaWs7nymW0mTexPJnvN3ild4GaVINtOhytaCsmKe+7tp87K6Or3x2S2Dmz64fqnAqjggpaiMAoaaK12TilCvVEXX5M1ctCQLlthzPn1kmL4zuzH2x9XZWVF/8u0v5I8M1wMplufJBczzw6nnhsrW0iFLVVRHpsfIbT9lwdLt4lTqtvpc3/G4enLdbtW37gdqS/czdkVa5sNen7+kri9cUr+9/SMDjjJ8+Wy3ylW6lNwMxekrO2bVb3+9ue/mzZzcnB1O9UkH2lSGJx4wz19P/tkVS1X6ipVZtbFzu5FhVHqJpmmeqITQP93yr9VXN/6Lhl8nwfRvbxxT07PvR3ZscalYRTVf+jz24/js0271799/WP645/IbV8ZiPyAAgWLOKGCYv5767jFLlYel8ihDpdKM3DSyYGm+lJ6OO3+y6X9S/8Uj/6fauuZPm35tV26j2r7hFbtq+snceOyVwzDJYiZ5HUoojdO6dSV149M1am42/4NHntv0b65fKiym9JQDbYkwChhkZcvP8llpQC4hZ13+UeOeHtlh6fZyOuaJyhD8C4+Nqm/0/vcql+ny9G97O3eox9ftUjcWfibtt0I7xrjlsk4j/HgXM23eUlYf/WZdt1Lq69cvFU6bcG4ABIMFTIBBZMtPCaJSjerKmrdew54nmpIguqXrGfXiE++oh9d+r+XvIb+nlSxmMqHVU++mBbXjqQX54/Dgkf6hlJ5uoC0RRgFDnJ76/khFleyLrKxS7zZwD3oJouUU7LAUZIiU6mraA2lndr0RO3/plfWK1fVAuhBGAQNIT9GStXBcqlCmVkXnStfVcmXGgCNpjRNEg+zb6gRSWQSVVhJI49azrqQGdtq7fPXRDB9ID8IoYACnp6gytCoqje3nStcMOJLWSFgMOog65Hv+V4/+38ZuTtCqfHat53m1Yfja01/a/UeVUocYrgfSgTAKxOxvruyWC+ohpedkmlYVlWNKQ2P7KMKiVF2ff/h4aN8/bp25+Kujsk3o15++4/zfk/EeDYAgEEaBmC2Ub9xNLyZWRWWeaBoa20c1r1Oa5UurqDTKZTrtZvhx+9rTt53qaN/gkf5jqTzZQBshjAIxenvqu4cyKjugDK2KpmWe6PMPHY90gdH3tr6e2gVNXYZsE1pVHT06eKR/IN4jAtAKwigQE1m0VLaW7y7CKFUWjKqKpmWeqOyo1GxXpTCkdf6oNMLvyK6N/Ti+tfOOWr/+bu/T9M6NANoAYRSIyWzpE1k9b5dCpSraYcBqZYccj2z3mXRSnZQqZRxksdTgA0cSfw5r6cxtsPuPxkmmjnx74G7VfmjwSP8hc84QAC8Io0AMfjj13FBGZUecnyxzRdfmtxrzVEgQTXo/UalKymKiOKuTMne0lab6pjKlEf6T22/a7Z60o/QeBZKJMArEQHqKOj/VUhXVkelRGUN255V955fKBQOOpDVSlTRh3mZcldmwdWR7Yq+Oim8N3HL+2MtwPZBM7E0PREwWLSllVVVFF9W6jkeNuLDLvvMrw/Px7kPeKqlGfn/b/2bEsUjFO5PJqOvzlww4muDItrUqY8VeQd+0eVl9PLleFZft98/AI89t+uD6pULy55gAbYTKKBAhWbRkWZW7i5ZMq4reKf4u8W2c7OH5h8wqkMlwfRp3ZzJlm9Cq6qiiOgokD2EUiNBs6epRS1XuzmuTFfRduc1GPAWzpauqVJk34EhaI8PzpgU/Cch/uuVfG3AkwTNhm9Cv7Jitnjs6QO9RIFkIo0BETk99fyCjcves+JUG4tJIPG7Sxmm+dCPxLwUZnje14by0l0pj71HZJtTA6ujBwSP9ffEdDQAvCKNARKoXLamV/6/W5LbFfvrTst2n0s3tTZbWxUwGVkdZzAQkCGEUiMDpqe+PWKoyVP2TZJ6oCVVRWbCUhu0+TRyeX00qt2ls9WRodXR48Ej/UP2vBmAKwigQMlm0VLIWjlavlpcVyGsM2G1psXwzFW2cJIQmZT/4wS0pbYRvXnVUUR0FkoEwCoRssXxT5oneN38t7h2XJBDPlK7GegxBeXbrscRsvZnu6mhH7Mexqjo6wM5MgPkIo0CIRie/01eyFg5W/wQJgSZUkWZSMjwvwa5v3Q8MOBL30lod7cptjP0YalRH2ZkJMBxhFAiVddTZf94hC4a6Yx6il12WliszLr7SfElcFJTW6qjMgc5lumI/jlXVUXn/Ha3/1QDiRhgFQiL7zyulRqq/uwTRrmy8RRrZZWmudC3WYwhKktslpXbuaM7IuaOHBo/0D8R3RAAaIYwCISlbS/dVY0qVRdWVizeMpmGXJZWCRvJUR8O1qjqqWMwEmIswCoSgVisn2fpTFnnEufXnXOl6KnZZEt/c/D8nfovNr274FwYcRfAMrY4ODR7pH47viADUQxgFAlarlZPSW3+uyT0Y2+lO0/C8VEWT0sqpEZlmkMY9602pju7ced+NF9VRwECEUSBg8+UbNVs5xb31pwzPp4VURZPSyqmZNITqWjqyPbEfQ9+Oglq/3rrnP9HqCTAPYRQIkFRFK9bywdXfsWwtxrpwKU3D82mpijqkOppGcvMV965MMje6f/vt1f+ZVk+AYQijQIDmSteOr27lpOz5olZsTe7TNDyvUlYVVTpcpzWQmtBP92tP31adnZXq/0SrJ8AwhFEgINLgfnUrJ2VAO6c0Dc+nrSrqSOtQvQl71ksQ/frTd1b/Z2n1dN9UGgDxIIwCAclm8idrfSdZuBRXk/s0Dc+rFFZFHdIrNan9UpsxoTr6jWdmVVdXZvV/pjoKGIIwCgRAGtyXraWh1d9J2jnFFZ7SNjyf1qqoI71zR+OvjuY7inarp1VGBo/03/eeBRA9wigQgIpVPL66lZPSVdGu3OZYTnGahudF3/ofpLIq6khrGFWmzB39xpe1/jPVUcAAhFGgRdLgvqJKNbcajKudk+w9n6bheZHk3ZbckKDdt+4H5h+oDyZUR6UBfo3qKI3wAQMQRoEW1Wpwr2Js51S2llM1PK9S3Bx+tSfX7TbrgAIkgTRuNZrgKxrhA/EjjAIteHvquzUb3Ct76L4cSzunmeJ0Kvaer5bWbTNXk9Cd1qkI0gS/1k1blLp75tS2hxZX/0RphH9fFwwA0clzrgF/pMH9fOnTmnPOpJ1TdwxzRRfLN9VyZSbynxsmWWX+8NrvRfKzbi79Sl2fv6SWK3fUtfn/eM/fdWU3qi3dz6h1+cfs38Na/S5zY397+0ehfO84SRDtyPWo5XK8r89vDtxSn73/8Or/LO/jU/EcEQDCKODTXOnaId1A+z6lyqLq6Xwk0lMrAXimdDV1T2fYK+glfP72zo/U9Mz7dghtZHr2/bt/K9MGJCRL1TbIsCxD9WkMo0pXR+MOo1IZfeThirp2/Z4qrVRHj11+48qx+I4MaF/3NV4D0NzY9Et9s6VPPqy125Kyh+hLan3HE5GeydvFKbVULkT6M8MmQ9avPfXrUH6KBL6f3fzf1Uyx9QAvwVQWWAWxIl4C8VsffaPl72MqeY0WY15c9/HkOnXpJw+u/s/y5um//MaVdL2JgARgzijgw1zp2tF6QbRkLag1+QciPa3FykzqgqgKqd2RhNAffvys+uDTw4EEUWXP071qf79/O/1f25XWVqR5Vb0ypM2TrKqX1fWryPv5UNzHBrSjeHttAAmkt/2sO7+sokpqbW5bZA9MhucLyx/Zv6fNf/7I/6G6chsDeVQSEv/dH/5H9evbf9V0ON6vhfLn9pC/fP9tawZVLtPl6zvJ9/lkbjyUY4xbJpO1X6sVqxjrkWQzeXXtD92r//PAI89t+jfXLxXuW+UEIDxURgHPrLqNsuPYh36+dMNu55Q2skAoiHZOEgwv3Tiqzl3day9QisI/3voLde73/n9eVAu24mJCm6e+7QV73/pVemmED0SPMAp4cHrq+9Lcvm4bmJUdl6ILo2nb8rNaEEP0EgYlFEo4jJrzs6sXPbkVVBA3lWwE4bdqHBQJok8/U7MAemjwSH/Ndm0AwkEYBTyoqFLdBtmyD30u060yEc5+mS1+EtnPilqrYVTmhsoczqiqobVIVVamBvhZHR9W6yhTyMr6uPVtv1nvCKiOAhEijAIu/XDquaGytTRU76vL1lKkC5dky8+09RR1yAKeVpq/y2Ii+WUKORavgTT9Q/XdRmwRuuOphVp/NUJ1FIgOYRRwqWwt1dz2s1o+E81cOJmbmtbhedXCtphSiZRqqIl9Or0G0rRXRpUhK+tl7mgdVEeBiBBGARekKmqpSt2qaNQLl+6kcMvParILkVcyHB/3sHwzXgKp7PKUdrlsd+xbhEoT/Icerrmyn+ooEBHCKOBCs6qo7LgU1cKltPYUdUhF0OsQvbNYKKi+oWFyG0jTukd9NWeL0Lj1Ux0FYkUYBZo48/HQSKOqqLJXB3dFtnBJqqJp5rUqKsFOgmhYvUPD4DaQpn3eqOiIaGpLI9IEf/16q9ZXUB0FIkAYBZpYrsw0ropGuOPSXOl6KnuKVnt4jfsAJoFOgl2SgqjDz6KmNMpkcqrDgL6j/dtv1/srqqNAyAijQAOjk4PSU7RhZcSyKpEsXJIQOl/+LPVPl9tqoOwrb9KKeT+aBdLlcvJCth8mtHn6xjOz9f6K6igQMsIo0FjDqogsXOoMaLvKZmZLV1O9aEm4bfQuIe7yF2+EfjxRkMciO0StJvNfTV6MFaRspiP2Jvj5jqI9XF8H1VEgROxND9Shq6J1d1tS9sKlebWu49HQVwTLoqXZ0h9S/1TJCvJGze5lOP7H1/5XNTXzbqTHFbYbiz9TH905c3e6gfz5b28cS+T0A78y9pSXeLeEl3mjH/1mXa2/kj3r37p+qZDelYNAjPKcfKCuptWQbKYzkoVLaV+05IZUCT+4fji11UKphKal2uuH7FefrcyoSozV/95NC3abp0+vd9T6a/k8OBD9UQHpxzA9UIObuaIyhzOKdk7tsGjJcXPxVzWrgc6K+XYZtm5XeSMWMtUtfjJ3FAgJYRSorWlVtCJhNORG9+2yaMkhQdQJnfLn6dn31bmrexO7Yh7emLCQqUGbJ8XcUSAcGc4rcC9dFT3Z6LRYqmK/fdblHw317Mnw/GL5Js8Q2oZs6FCszMf6cP/x5w+on39Yd6vS/stvXGHeDBAgKqPA/ZpWP8rWkurObQr11MmiJYIo2o0J1dEGQ/WK6igQPMIoUMXNXFFH2L1FZ0qf8NSg7ZjQ5qlnXUnteGqh3l8zdxQIGGEUuFfTqof0Fu3I1h3CC4RUREsxD1UCcenIron93H9lx0yjv27Y8g2AN4RRQHNbFS1VFkJduCRhd6Z0lacFbUtW1Yfdu7eZB7fN2RXSOg4OHukPv5UG0CYIo8AfuZoLJr1Fc5nO0E7bXOla6ndaAprpyMU/d/TpZ+bq/ZUE0UPRHg2QXoRRwENVtGIVVWeIQ/R2K6fSDZ4StL2OkNniJBgAACAASURBVOdku9HXeCET1VEgIIRRYIWrqqisog+z0f0MOy0Btkwmp/LZ7lhPRmdnpdF+9b3MHQWCQRhF2/Oygj6X6Q5t+09p5bRcabhoAmgrYXescOMbz9QNo+Igr0igdYRRwHVVdFF15jaEdrrYfx64l1RGs5lwbv7ckv3qt2ypO4e7b/BIP9VRoEWEUbS1Mx8PjViq4qoqaikrtFX08+UbbbP/POBFR3Zd7Ofra0833IqWJvhAiwijaGsla+GomxYysv1nWCvopZWTrKAHcL+8AT1HH328YRiV6uhwdEcDpA9hFG3r9Md/Ply2ll1VRVe2/9wSyqmilRNQn9wsdmTjnTvaZCGTYu4o0BrCKNpWqTJ30G1jbcuqhLKYglZOQHP5mMOo2PFUwx3RhgaP9A9FdzRAuhBG0ZZ+OPXckKUqri4eMkQfVouZWXZaApqSKTJxL2RqsiOTojoK+EcYRVsqW0uu5ooqvf3nmty2wE+TtHJaKjdsqg1AM2EhU4MdmcTw4JF+V9N+ANyLMIq2c+bjoSFLlT0NqYWxeGmudJ0XH+CSCQuZvrKjaR9gVtYDPhBG0XaKlfmDbhvXy0r3ztzGwE/RYvkmDe4BD2QkI+4dmfIdRfXYEw3njg6zRSjgHWEUbWV08jt9liq7bsNSqiyG0luUVk6AdybsyLTjqYVGfy0fFoeiOxogHQijaDOW52G0oIfoaXAP+GPCjkzSc3T9eqvRl7CQCfCIMIq2MTb9kiwucL11XxhD9DS4B1pjQpunRx9vOMWmly1CAW8Io2gbC+XPPVVFwxiil56iNLgH/OswYKj+G880bICvWMgEeEMYRVsYm36pt2wted6yL8ghervBffkzXnBACzKZnMplumI9hWt7ltSmzQ2n2vTRBB9wjzCKtjBXunYoo9yXOcMYomfbTyAYHQa0efpG456jirmjgHuEUaSeVEUtVfF0YQh6iF6qotLOCUDrZN6o200rwiILmZqgCT7gEmEUqTdXujbspSrqCHKIfqY4zQsNCFDcPUc7OyvN2jwpqqOAO4RRtANPiwmCHqKXbT9pcA8EqyPbE/sZbdIAX4zQBB9ojjCKVHt76llpseJpqCzoIXq2/QSCl810GNFzVCqkDfR6aScHtCvCKFKtYhVf8/P4ghqipyoKhKcjuy72s7udoXqgZYRRpNYPp54bslTFU3uVoIfoZ0qf8AIDQpLPxDtvVHxlR9ObTWnz5LmtHNBOCKNIrYpVOuh1xW2QQ/Syer5UaTqnDIBPJvQc7d20oHrWlZp9ma8RGqBdEEaRSqOT3+mzVNlXNSKoIXq2/QTCZ0LP0WeeWWz2JbR5AhogjCKVMirreTs+GaIPag6aVEWltyiAcOWy3fH3HH2iac9RxdxRoD7CKFJHmtxXVNFzVdQeog9ovihVUSAaEkTj7jnqYntQRZsnoD7CKFJntnTV09aff2SpfGZty6djvnyDqigQoVwyFjLJZxILmYAaCKNInYzKeV4sYKmKygcw90yG+qmKAtHKGzBU72I3JsVQPVAbYRSpMjo56LnJvbL3jl9S3bktLZ+K+dINZVllXlRAxIK4mWxFvqOonnyy6ar6gcEj/QO8NoB7EUaRKhmV9VV5qFjFlofopSo6X/6MFxQQg45s61NsWvXoE642uKA6CqxCGEVqjE5+Z6iiSp6rDjJEH8ScM6qiQHxM2R7UBRYyAasQRpEi1mt+5o1VrGXVlWvt2kBVFIhf3AuZZJ/6x55wtdEF+9UDVQijSIWx6Zf6/H7Al6zWd12iKgrEr9OAveoff2LOzZcxVA9UyXMykAZzpWu+Kw35TGsLH6iKJp9U1LMZ5+Mwq3KZDl+PSRbCOSqqzA1KxGR7UBmulzngcXFZGZX96ocuv3FlPDUnH2gBYRSJJ03u50rXfFUapB9oZ3ZDS6eAqmgySEjJqJwdNCW0yJ8lgAbZEqhD1a7MOX1nZUqIzFGWsERYDYcsZFoq347t5ztD9Z/8vumCKmlBRxhF21MM0yMN5krXhnVDac/KMkTfwnxRqqJmkuDZke2xn9s1uQdUT/5h+/fu3CZ7y1ephucynZH1ppSfJb/kZ8vNj7QRW5vbah+X/Fn+mxyvfA1akzegAb7LoXoWMgEalVEkXjaTP1ixmvb3q0nCiFTI/KIqaoaVsNelsjr0Je/Y7z1mqaRK9XTl95VqKtxJ0FC90vPcT4R7NID5qIwi0X449dxQ2Vr21URaqpqdVEUTSQKHVBKl0ulUF6XqmJbK4koVdeXxrc1vs6u6ndn1VE5dirvnqIdV9SxkQttThFEkXdla8tXOSZQqiy2tvqUqGi2pdslw9tr8VnuIW/5swp7kUViZdrBuZXg/v82efiA7DsW9BaapEjRUby9kCv9oALMxTI/EknZOs6U/jGRUxudDsHzvukRVNBoSwmR+58re4/E2NDeFBFD7nEgXiOzKvGdpT1aquNobvS0kbKiehUxoe9xWI7GknZPfICpz8LItDHlSFQ2PBAkZkpYKqAxPy3A1QbQ+qQ5Ln1ynYspQ/goThupd7FUvhlnIhHZHGEWSveb32FvZdYmqaDhk2NlZZS5D0gRQb5yK6cpQ/lYd4tv3I96EoXqXe9XLB9Fw+EcDmIswikR6e+pZWYXa5/fYW9l1iapocP5YBd1mPx9U9YIhQX5lfu1KtTTrs4l/kjlD9XHaum3W7U9nIRPaGmEUiVSxir6roqqFXZeoigZDQqeEpD9WQfkoCou81ld6rG5pmwVfjriH6nvWldSmzctuvnRg8Ei/r64gQBpwBUDijE5+RyqivlegyqKGfLbH17+lKtoaZyhefrW6DSu8kRuAlVZRW+3noR2YMFS/4ynXC8tausEGkowwisTJZjpaGtKS/cP9DtEvlr/gBeODhB8JQQzFx0+G8FcWPKU/lJowVP+YuxZPSjfAB9oSYRSJIvvQlysLLX1oy0p6P4FosXzz7h7jcKc6hLIgySztEkrjHqpf27NkD9e70Dt4pJ+FTGhLhFEkiuxDn8n43zZJgmiHzyH6udI1XiwuEUKTwwmlMq80jVVrMxrge+o5CrQdwigSJaOyLQ/Rd2TXe/53VEXdIYQmlwxnO/N5ZXg7LUwYqn/iyUW3X0rPUbQlwigS4/TU9wcqqtjSilO/80WpijYmFTVCaDrYz6W93er61HQ5kB284vTgtjm7Cb5LzB1F22E7UCRG2Vo+2GrQySrvFZKlSoGqaB3OfvFhD+9+cu3+OXeLSxX1+c2VzgYPbsmp7q77g1NXZ0Y9+ADh2A9puSWV7qXyHXvL0SSTofpl5aoBfWieeHJJTX7kam6uDNWfSPQJBzzyu6k3EClZuDRbvHqllfmi0tIpJ8PIua2e/l1h+bdquRLvhcw0UjHrzG0IrD2ThM3bM2V1Z6aiPv+irJaWLHXjZsn+PUiPP7JyMyIBtasrozasz6qN63N2mJX/j9rkZkxuypLc1my+9JmqxHj8H0+uU5d+8qDbL995+Y0rE+EeEWAOKqNIhFYXLil9QV2TdX0xsBUrMwTRVWT4Np9d63sIV4Ln1WtFO3Te+KJkB9CoyM9VVb9XkzC6dUveDqoSUrc+kFePPcJHpNJD97LAqViZVcWK61ZFRpGG/xUrvmN/zP0iJqWro4RRtA0+aZEIsnBJVsK3xlL5jLc2L3Ol67xANGfXJK9TJSR0Tk4vq0/+UKoZAk0hVVg5vtXHKOFUQupjj+btyqoE1XZkV8PtKRndiaySypSDOIO0zBl96OGi+vS6q6lC0uLpcPhHBZiBcSkYb2XHJetKq8cpldENHe63s5evv7n0y7Z/gUgI6cpt9LSV5OSVZTV1pWiH0KCH2uMmYVRCqYTTHX2dbTm8LzeGSaySzhU/Va3f1Pr3219vVn//dxvd/vs9l9+4MhbbwQIRojIK48mOS5UWFxDJAozOrOuLgI0V9Mruyep27/g0B9BqMq3gV79Zsn9dUHN25XRHf6cdTNtlsdQ9VdLyrVgDnheyqr5Y8TRcHqgHt80qpVx/Dr0i0+VjO1ggQoRRGK9sLY602mJGqpxe+ovK10tv0XYlvRndbN0pwexnv1i0g2iUcz9NItMQPv9iQV36+wW7airB9JmvdbVFMLXnkuYfVEvl24lYcS/huajiC6ObNi+r9estNTPjqpouQ/UHwj8qIH70PIHRRicHhzMq03LfvYpVsvtgujVfvpbYhRqtkgVK3blNKtug8bksQrrw4zk1/tN5df2zklpaTm8l1As5D3I+fvFPK5VT+f+yWj/NQ/kZlbHnY8oNo/TxNVkmk7WnF8SpcKtbffmlq3mj3Y88t+l31y8VWMiE1KMyCtO1vD2epcqeml7L1y+0YVVUeoZ2ZTc23K1GApZUANu1CuqFnCM5V/JL5pg+/fWVimlayZQOqZQuVm4Zu7hJArN8FpQq8VVxZajeZb9RpYfqT4V7RED8WMAEY41Nv9Q3W/rDlUyLL9OStWBvceh25yVZQd9u80WlGipzQ+shhAZDKqR/+q1uO5SmdVW+zB+VeaSmbhQhIx4yrSAupWKHOj36mJefvunyG1cKsR0wEAEqozDWneKV4Vym9UqSDB12epgvulj+om1eFM2qoYTQYMnCLqdaKoH0ma93pa6XqVQf5eZPhsNN7NEruzEtqfjCaL6jqLZsKaubN13PkhumOoq0I4zCWPnMmgB6i8qihS7XvTFl0VK7bP3ZaKW8zAl9/8ezhNAQOSvyZQj/e3+2JnWh1H5tZXJquXzHqNX2ckwyHzrO3Zhkr/qbNze4/fLXCKNIO4bpYaTTU98fKFpzH7a6il7mf2ZUXvXkH3b19dJXNO1hdKVv6KaaK+UlfMrCJJOb06dVWkOpbMNr2jxSGaaPc4HiJ79fqz748TYv/6T/8htXpsM7IiBeVEZhpLK1fLDVICpkoUJP/iFXXytbf6Y9iEprG2lgX+vcXvqHleFjxMPe/endYupCqUwBka1EF8tf2sHUBDJaUlTxhdHHn/TcdUCG6k+EczRA/AijMJKlysNBHJeX/qLz5RupfTFI+JRhUxmaX40hebOkMZTK608CqWwjWqrEf8Njd9eIsVArVWIPW4MqPVRPGEVq0WcUxpHeokqplnuLKr2yVxZTNCOhdab4+1S+GKQyJX1DV7e3ksU0P/m7BfXv/8McfUIN5Oz0JL9vfSCfil6lsnhIumOYMAJRUUW7/3Bc5uc61Weful6g+dAjz2166/qlAqvqkUpURmEcS1VeC2KIXoYEG7UrqpbWVk7SjFy2bVx9PmXXoHffn6EamgASSGWLVWkJ9aff7E58KHUWNi2V481VMlRfUvH1G932kFSI3Xf5YKgeaUYYhVHGpl/qnS99OhzE6lupvnTnNjf9OlnkJMOHaWLvHZ7bIB0J7ntUzA0NlbyQnB1z5PfbVX9e/SIrXH7jyt3ddQaP9Esj3IEaB9e7tGQNyHP2yR9K6sAr33utt+eBvjvFaVWuLKq50qeJO0nyuszkMvZCorhW2sfd4mnrQ56DMEP1SC1W08Mob330zEg+030yiGNaLN9SW7qeafp1aWtyX693qAzLv/f+LCvlgzGulJLVzb/Tf74nWIZt6s57x5RSByWoKnt1eMG+oZovfqpK1pKSoGr/t5irj83YK+3LX8YWSOdLn8Xa4un/vfCYl3mjilX1SCsqozBKPtN9MKjjqdW6qJY0Nbmvt1peFinJsLwEUngmYfMDXd2cjjJ01rN9w8vHpu68J70n5cZtqCvXq+TXho4+/S+et/+3bK1UTu8s/07N279Pq5IV39D0aivzmTfHFkjl/VKx4ltV37tpwWsYZageqURlFMaQ7T/nSteuBHE8UnHJZjqb9heVJvdSRUoDmRtaa7X8z36xqMZ/Os8L3Z1CVfgcNyF4NjN1571hHUpd7XfrhNI7xd+pL5f+OdZjd0gQjaP1k7R+k58bFx/9Ricuv3FlZ2wHDISEMApj/OVHf3Iol+k6HsTxSENr6S/arK1TYfm3Rm5Z6EW9JvZSBZUQKgtg0JCEz3eTEj5rmbrzXq8OpJ5bot1a+mf15dJv7N/jrJrGEUjlZ84V45tz62OfesVQPdKIYXoYI5fpei2oY3HTX7RkzSc+iDrDnKuH5SWI/ui9O/aqedxHqp9jVQE08avXtm94WR7Dnqk770lLtONuq6RiU9fX7V9KvRJrMF3Z035zpIFUfqa8h+Jqxu9jn3rFUD3SiMoojLCy/efsh273kG9GKqO9nU81/CoZnpdh+qSStk1d2fszB22bapp2AujlN66MG3h8gZm6816fM5e0le/5+eKEurX0m8iH8qOukMoN6XI5vpvSv/vpQ2ryo/u7XjQgN1C7oj5OIEyEURhhdHJQqjmHgjgWN/vRy9d8sfRLo/bL9kIWq9Rq2yRBVCqiLFSyORXQt9IeQGvRK+6Ptvp9ZEX+54s/t8NpVKvzowykMoqyUIpvEeM//9MGdfn/a74xxyqb0lDRBxwM08MIGZUNpLeouttftPEQ/UL5ZiKDaL35oUo3R7/w4/hWBhvECaBj7XwS9Ip7CeFnvQzbryY3Po/1PG//kkAqwVQWQIUpyiF7t103wiLbgvogQ/WnYj1wIECEUcRuZYh+ri+IXZeUHUaXVGeT+aILpc8S98Q723rWmspAELWH4d+UIMrijj/avuHl8ak77/XrQNrSsL14sHvA/iVTXD5fWKmWhiXaQNplf27EQdo7+fAKYRRpwjA9Yvf21PeOV6zlQIbolT1fdFb1dn617t9Lc/Dby1OJeuLr9Q9VBFGpfr7ZjsPwXgU1bF9Nhu0/mfsg1AVPUQzZxz1v1Efze8VQPdIkmFIU0JKK53Y09ciFK9tk2C1pi5akd+hKRZQgqskF+HXd4mYPQdQdGbaXFfc1tiX1TYbwt294Re184KB6rGfI3mIzaE6FdPWOYkGSymicNm1e9vPTW650A6YgjCJWMkRftpb6gjqGirXccIhe5pOavkViNbnYSzP7WtowiMrw+wEdQo8xHO/d9g0vSyV5V9X++YGQyr3MKQ0rlIYdSOOeN+ozjL4S/JEA8WDOKGJVshZeC6qdk3LRXzQpVdFmF982C6LjekESc+QCsH3DyxNTd97bFdQ80mpOKH147XfV9fm/U5/O/21gw/dhzyGNc97ops2+fu6wvjkDEo85o4jVX0/+2RVLVQKrjMp80M2dT9f9+5tLv7QDq8kkgHZlNxJEV0Lo6wzDh2fqznvSj3QkrB8ge+MHHUrDmkMa97zR0VP9fv7ZzqTuGgZUY5gesRmd/E5fRRUDC6KqydwvCapJCKJURO0QuksaexNEw7V9w8sHwqyuVQ/fyyr8IIQ1ZB/3vFHZicmHwHatA+JEGEWMrOGgh+jzmbV1/970IXrZUWlN7oGaC5VUewRRQmgMtm94+VTQC5tWk1BqL3TaEkwoDSOQxj1vdE2Pr6F6FjEhFQijiE02kw/0rl6G7Tqz62r+nekLl2TFfK2tPR2ys1KKgyghNGZVC5tCfZM4q++/tflfqQ2drQ2KOIE0kwnuhjbMFfvNbPa3iGlg8Eh/oKNLQBwIo4jF2PRLfWWrGMy4ndZo8ZLJVdFGK+ZV1RafKTRNCDWHLGwKY6V9LWvzD6mne19TT296zX79+2UH0mzttmd+xFkd3exvmF7phUxAohFGEYu50rWhTMDr53INqhqL5fj2nq5npbKzqeYe8w7ZYz6Fe83bLZouv3GlnxBqligDqdjQ0WcP3fet2+27HZQzzzqIQBrnvNGedb4XZD0f7JEA0SOMIi6B9sizVLlus3sTFy45Q4y5BhfgFAZRp1n9Tto0mWv7hpcLUQZS8dDaZ+1FTg+vfdbXvw8qkDa6oQ2bz21BFZVRpAFhFLGwlBXoB+jKfNFkDNHLhXNN/oGm89Pee3/WHqJPiVNVzerZwtBwTiAtVmbHojpSuTF7ct1u30P3QQRSmX8a1JC/H+vX+7vxHDzSTyBFohFGEbnTH//5cNBD9BWrZK9GX00qpiYtXPrjBbPxogtZrHT1Wnh7cUdoXFdCDxBCk0UC6dd7/9s9+kYiMjJ0Lwuc/FRJ5f3Vmas//9qNXDa+eaNr/a2oVwzVI+kIo4hcqTIX+Aen7JxSq63TgkFVUQnLbio30sJJfiVcQc8L3UVT7mTTvUgjDaROlfRrG/d7nksqc7BbWRQV54p6v5VRWjwh6QijiEPgQ0r15l6aMkQvQVRaNzULoilp4XRCD8kzLzQl4gikYlPX19U3N/8r1ZN/yNO/ayWQxrmIqYXKKC2ekGiEUURKWjpZqhzoh6YMxeez94fRkjWvSpX52J9gadvUqIeow1mwlGDOkPxhhuTTJ65AKqHymz6G7f0G0mwm7/nfBKWrq6XpS1RHkViEUURqpng10F2XlF68VGuI3oSqqFwMpaG9GwleOS/B8zBD8ukXVyAVMmwvDfO9DNtLIK23sLEeGb3IBthI34uNm1q6eWbeKBKLMIpIZTO5wD8w6y1einO+qFzQJIg26iFaTYbmE7pyfkwPyZ8w4FgQgTgDqWwl6nW1fUd2Xc3Ph0binDfaAlbUI7EIo4ha4ENJtRYvSW9Ry4on3Dk9RN0G0YQuWHJ2T9rDkHz7iTOQyu5N3/I4j1SmyXgJpHGF0RaH6XsHj/QHuqsdEBXCKCJz5uOhIUtV/C9zraPW4qW4huidIOr2YibV0PGfxj+v1aMTem4ouye1sTgDqbznZR6pVErd8hJI41rEtMnf/vTVmDeKRCKMIjJLlcJQ0A2lay1eiqu36Eoz+wddB1GZH/r+xdkkzRN1qqEsUIItzkC68vNfUY/1uM9fbgNpNuB57W4FMJrDvFEkEmEUEcqEMF+0qLKrqhhxBVGvu79IRTRB80SphqKmuAPpYz3P26HULTeBNO6dmFpAZRSJRBhFhDKBf1DK4qWOVXMzox6ilyFDr0F08spyUuaJUg1FU3EHUmdhk9uV9m4CaUIXMTFvFIlEGEUkZL6oDKAHrWwVVUdV65aytayWKzORPakruypt8hRE78xU1IWLiWhsTzUUrsUdSGUb0SADaZz9RltEdRSJQxhFJJYrMwNhDHtlMvd+T1lFHxVnVyWvpI2T4fNEC1RD4YcE0mJlLrabF1lpL4HU7Ur7RoE0wWGUeaNIHMIoohLKB2Qu03nP/49qiF76HPoJopf+YUFdvVYM5ZgC4vQNpRoKXzqyPXuUUrFtfhBUIE3oML2iMookIowiKoHPY5Ih+er+ovL/o9j+00sz+2qyWOnS3y+EeWitkAroAfqGolXbN7xsV9bjDKQyj7vVQBrHivpMMDs/MW8UiUMYRehkP/qKVQx0P3p1t63THy8gYQ/Rr/QQ3eQriMqw/LvvRzeX1SNnT/nY5vshXZIaSKu3Dg0oGHpy68vOoL4VYRSJQhhF6GaKvxsIY8hL2jpVD9OHOUTvNLOv1WDfDRmel4VLBnpd7yk/beLBIblMCqRum+PL1qHVW41GPVQf4Fxy5o0iUQijCF020xHKXbqEUWeYPswheqmQeNlVabVPrpXUz36xGMqxtcBp2XTMtANDeuhAukdPA4mFBFLpQ+o2kMrIh9MhI+peo7dvrXXxVa5QGUWiEEYRhW+H8TOqw2FYQ/T2rkq5B3wHUXuXpR/PBn5cLRqjZROisn3Dy9O6QhrrXGQvgdTpHexlP/sg3LwZ2NSAgcEj/YFvvQyEhTCKKIRyl14dEMMYovezq9Jqhg3PSxg4zCIlRG37hpcnkhZIV25Et4R+TNU++9TfNKA6qI4iMQijCJ2lyoEvXqpeSS8LmYIeopeKiFREWwmihg3PT+hh+RMGHAvaUBIDqcwfnf2iTy0vh3+pLNxao+ZmA+1tSosnJAZhFKEam35pSJb/BE0CqLN4Kei96P02s69m2PD8KR1EY1tIAqh7A2msvATSG5+tVeffe1zd+DSw+Zw1/fpX64L+lqFMjwLCQBhFqL5c/nVvGIsALKsqjAY4X9RvM/vVDBmed3qHHmBYHqbQgfRA3IfjJZDOzmbVv3t/m/rbn2wNpUoqw/MfTwYeRhmmR2IQRhGqtbmtoXwgyp70+exKpSKIyqgEZr/N7FczZHjeGZandyiMs33Dy6eSEkgf3PLHRUVTkz3q3XceV7+c2BRYKJXeov/hx9sC+V6r9LGICUlBGEWowuv9WVEZlQukKur0EA0iiIqLP50L5Pu0gGF5GC8pgbS7697LpITQX0z0thxKZX7opZ88qP6f9x4Nc04q1VEkQqCzpYHV1uQefL5kBb8FZi7TZf/ealV0ZcX8JjvYBkGG52Xbz5g4q+WphiIRJJBO3XlPDvVknMcrgVR8vuj+/s0JpfLrsSfm1eNPzKltDy2qnnWluv9GAqgMyV/9fY/65PfhzkHVhvQOa4DRCKMIVRhB1JKqqN6qr1jxv8WmzDnt0s2tgyBzRGPce35Czw+lGopEMSWQ9q3freZLn6q50qee/60ESydcShhdVyOQzs7mg14t78aTUf9AwA+G6RGqkhX83MmKVbLbOkl7J/nlh6yY785tCXSHlQs/jm14foxheSSZCUP2Xveyr8epfq7+FUMQVQzTIykIo0iglcqo3/miQa2Yr/ar3yypq9eKcZxKmtgjFdIUSA1CGEUiEEaROFIZ7cis8TxEv7JQaUtgC5Uc0lN0/Kfh7IvfQEFv6UkTe6QGgTR4g0f6A990BAgaYRSJ48wZXfYQRu2t/fIP3O1NGiQJohJIIyQLEvoZlkcamRJIv7pxv8pnuuMa8QgSYRTGI4wiVNkQ1shVrJWLgzS+d2NlfujmwFbMV5OeojJEH6HXL79xZRfD8kgzEwKpTOeRCqkE0oRjW1AYjzCKUOUyXdNhfP/lSvOtNp1G9jI/NIxdoESEW35K+JS5ocei+oFAnHQgjXUv+7X5h9TWnq+NJfyFsNGAYwAaIowiVJYq/y7o75/J5JvOF13pHxpcI/taItzyc0LPB/6UcQAAGw1JREFUD036RRHwZPuGl8fjDqTf/Nbym7JQMMHPHIuYYDzCKEJVshbHZI5nkLKq8XzRjmyPHUQlkIZFQmhEW346uymFUmEGTKf3st+pb8oi99ITfzOuFwomdTMJtgSF8QijCNVrT/1qwrIqge0AIsG2bC3VnC+6slp+k+rMbghtWN4hPUUjWLQkTewPMD8U7W77hpendYU00tGBUmX+bgCW92JCAymVURiPMIrQFSszh4Oqjkpbp1pk9eua/IP272GbvLIc9grbaT0sz7aegLZ9w8uF7Rte3iOL+KI6J/ns2req/3+CAylgNMIoQvc/fO3jCaWswC4g1bsuSYsn6R3aHeC2no1E0FN0XAdR2jYBNWzf8PIxXSWNYurKfZXYJAbSwSP9rKiH0QijiMR/t2PiWKkyH8AH+EqFVYJnZ3a9WpvbGkrv0Hp+9svFMBct0bYJcEEvbNoZcig8oacH3EcHUhYUAgEhjCIyr3311wfK1lJLF4+KVbZDqAzJd2TXRfrkff5FWV36+4UwvjVtmwCP9LD9gZCqpBMupgMciGtRFZA2hFFE6l8+9Y8SSA/7bNUy0ZHtmZYQGsWQ/GohDc/TtglogVRJt294uV+HwyBCqayc3yVht9EX6RGMXQkJpCxigtEIo4jcv3zqH+XDvl9XHppdPKb1xWHnqzsu74xonth9ZJelEBYtnbr8xpWdtG0CWidN8qtCqdebu4L+nOnfvuHlw82CqCNBgZT2TjBahqcHcRudHBzQd+7OHsoF/eE+8eqOy/dcFEYnBy9Gvb2dLFr6i9FCkK2c5DEdZrU8EJ6pO+9JABtWSn276vPF+YyZ0O/DD2TQQ89B9W3wSL/8rIsGVyBfZxoQTBb8xuGAR6/uuDxhcmVBhucDDKITun8oc82AEOnqZiQ3fFIhHTzSv8vwQAoYi2F6JE2kw02fXCvZQ/QBcXZTIogCKZOwOaSAUQijSJpIqw7v/3g2iG9TYDclIP0IpIA/hFEkTWRh7tI/LATRU3RCV0OZHwq0AQIp4B1hFIlSuF2OJIxKCA2gpyjD8kAbqgqkprRsY0QGRmMBExJlds7q690Y/hFf+PFcK//cGZandyjQpnQg3TN4pP+kUmok5rPADTGMRmUUWOVnv1hspafoOE3sATj01qEnOCFAfYRRJMq6nkyoDeLt4fl/8D087+wtTxN7AHddfuPKYd2MPxaX37jSUh9VIGyEUSTK7JwVatCT4XkfPUWdLT1pKg2gJr2IcVcM8zcZoofxCKNIlHLZCu2D3Ofw/AkWKQFwQ1cod0YcEKmKwniEUSRKxbK37wvc51+U7Z2WPJjWIfQwvUMBuKWn8eyKcB7pWzw5MB170yNRRicHZW/pK0EeswzL/+i9O3Ygdel1uZAQQgG0YvBIv+ydfzLEneXGZR47TxJMR2UUifLqjsvTQQ9xSUXUZRB1VsofI4gCaJXuutEfYpX0ME8SkoAwiiQKbNhJ9p13sfe80zeUuaEAAiU3tnq1/c4A53c6n1l8XiERGKZH4oxODvbqofqWhrakGvpXZ243+hL5QH+TIXkAURk80j+klDqolBr28SMLeue31/nMQpIQRpFIo5OD8kF91u+xSxCVeaIN2jjJBzqLkwDEYvBIf58OpM8rpYYa3HwXdEX1Xdl+lM8sJBFhFIk1Ojnoa5s9CaB/MVqoFUSdqsKbNK4HYBIdTvtWHdI0n1VIA8IoEm10cvBDpdSA28cgFdF335+xd1qqMqGH46kqAAAQsTwnHAknbUsuugmkn1wr2UFUV0RlWOsDHUCZ5A8AQEyojCIVRicHjyulDtV5LIWZ2cqp/+uvCj/Xw1rsSAIAgCEIo0gN3RD/4KoqqUzqP/XqjssMvwMAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABtDwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEA6ZEx6FOfOXxgK6Fv16V9+FJRSEy++sHs8oGMJVI1zVOucyWMYe/GF3dMmPoakOHf+Qq9SaqDqcIN6fdYyoZ83MR3Xc6dfX/KYe31+i2n9q5FWzuOEfn/y2jbY3n37q59jN8939evf9s6Z06F9Bq86PuXymnH3GMM8tiCsenytvJ+bqX6/F945c3oi1geOxIoljOqL/LBS6tv6jRLmRd4vO9AppV6P+sJXFQie1L/7DdcHXnxh9ymXP++1qp/xrlLq1Isv7C40+aehOnf+ghzPwapAKB90b4bxfJw7f8F5HT6vz8OAi38Wtgn9Qf9zpdR4WDdI+v14XL8nw7poBW1avzebvr7DsHff/tU3Kq3cAK92z/OcgOAzoF87zwd8HpT+HJbH/4F8Hr9z5rSn975+npz39UDAwWxav0fl2MbjCGJ79+3vW/W5ZcK1tFB1Xib0uYn1WlKt6r07pK+xffo1EdVnfmH1ZzshPuIweu78hREdekwMn/XIC+dwmBc9HQZG9AfKcMDfflejEHPu/AX5eWdr/JVUn3YGfCyu6SD6YY0LhzwfO4MIpPpnjKwK4qaTG6R3g3o96uf/ZIJC6Gry2t4T5o3Tqgv+QEw3Ks4FrDr8xHaB1+fkoH7/RPnakcf/ltwsN3r8e/ftl+N6JYTP00bkM+nNZscWBP34Dhpy0+yGq+ctLPr1Oqw/6008Z07x6y0Tbj71DWZ1Ueb5qr+uFdyd4z/s9/mNJIzqytvJBF3wa3FVZfRCn5eDIX9gSoXzQL2/PHf+wsUGNwcS+mK5Yzt3/sIxpdTROn8tNwcnWvjevfp7H/J/hLEr6AvfCb9BTN8cnkzwOXBM6JuuwC5yunoyYvDFS1VdvMai+oH6on5Un5s43X39Oxc//Zwd0p+pcd5c3XdsQdEh9HiCbx6FXEdf91rl9kNPVwj7Ghu0cX1+Igulq0Y3WppG9c6Z076KWKGH0XPnL5w04IMrKFKBafmDX1fkTkZUIZbh3V0NjsVq8G8Debx+NAnJEsAO+/y+Q7oSnOQP82rTOpx7ep70tIQPQz+66ARSydeB5njCPrOm9cUr1CkLe/ftP6TPjUnskSv9u2kVfnle9gQxBKtfl2cTNqrYjLxmj4XxjfVNU1TX2LA4lcZQQnvVzVvQI4N7/NwgZwM8gPukLIiKk7qq5puuRn0Y4ZuklZ9jakXI13Hpc38xRUFU6Q+Rs+fOX/AaEmpNzUiyAV1N902HrSsJ/MyyL7x79+3/UFc4Ard33/6TBgZRpd/LJw29wZTn5aKuZvqmn9MrKQui4ujeffsv6lAUmL379h9LyfmSSqW8pwOv6lado6MhjFj7+gwKLYymMIgq/WHne2hXn5Mkz89LrKq5kWl1SL++mtKhPMlTZuo56OdmUS6Ge/ftP5uC4c+BIMLPajqIpu2zPCq9+kbBVzDSQS1tN9DVhvRrtuXHV/U+rje9K4nsinhQ72m5sZGbVn2OwnpNPe/ia+4TShjVQ6Fp/fDydcHTlas4zknbr9LTz1eag6hj5Nz5C25ultL0YV3N881i1cU+SXPKGnHCT1AXr6RNWTDVWZ+BK01TiuoZaHWkJoXv49Vafk/rCvtFU0c8w6qMmjicE5Rery94XZWLa7HMuzH9XJMkveLlxXE9J7km/VpMY1XU8ZrHrz+boBXJXgRx8RpK+CI/k3i+IdbDs2kbmq9nSE+T8Sut7+Nqx/VcWM+qgmgU10Ffi/byQR+FXhjh9UUxrVeQ/byqkldwVnLri2tUF9Bevfqu0YfAQb0isKmqHo5xkHPqe9V5GlS1b2on8nrbU+fxeg1rqqrP432NyUPk9Nn1Gp77ZBqCm84Xet5Umi/2cvGaaGEBTVor6HEZloDvYZV0u51/mUPqufVTG7yPHb1NPtsbiXJ64Ad+/lHgYdTjxW5cN65u+ObUPSWjbDw/1mTOqyyWGHDZ9iioSlShKqjL77er/u7bq15odi/CuFbCG6bVYZvpqubWzjkPI5RVf5hubLFB97CE8NW9WKs2m3Cj5dZRLXI+Ew7rG9yjHo79lWY3i3pY72CLx1j9ubT6PdkKZ85Vq424nYtX3W4a9ehKSqsXeOd94uXi5HyWhd2EvPrzVJ7D3zX5+ier+i62clF/bfWmBrXo89/K469urO48tjCuo6vPh7ORjZ9rnvP55LorRFW/23YhNzR9XlbY67AeVdV4wsvzVy2MMOr2QUsIDaWtQxCkN6ee+1rvTTXgcj6m37tbp4nsu7o9kzE7WCTMKz4Od0IHsfEId9+qeYHSQeygj+rucI2quNv3ZkH37TRivrE+jj0e+qK6CVGHfISKad24ezyGHoBDPnueDnmsxjn8XOAL+kL0blDnRw9VNxupivTYqjZB8LOBy8jeffvdNAb383inq3rPRvXerXku9Tka8dHztemN5CpBLMRxbpqqg3uhxfUW1aO5Qd3IOA7qdmZevj4Iq5/r6sczUdUf1YxhepdvolMmB9EqYw3mTDW982thesEJHdYJoK3z8qFe0L1Vjdl+UQcxuTF60+Ocn+drhFG358KYIFpNht71e6rZDZ6bc+RlBEcuUgfi2hlFBwv5dULP4zzuMZQedFONW8VrGAqlZ6TuVzjWYo/TU63sDFPjmKb19zzl8/kY0teWRryuSA6tZ6cf+hwd27tv/wmPi2Zcj2Tp0Q0/I18TTpEn6vd01Vakr+jXgZ+Kpev3pr6Z8xOAx/U5mojqHIXaZ7QBXw3LY+Br7kMVP3e3stPTYYJo6xot5Kmh0Gzr1DjpcOhluNXv1JBTJgZRh76JbfreaNTxomqrOzckdOw0ZX94OQ69w4mX6tGwl5Xcuqrl5fWzK+wg9M6Z0ydcBLhaZBekA2FtQalfF7s8VtHcBBAv5/+ASUG0mj7vu7xMD/CwSGfIY9Ca0K9VeT8fi+M9LedDv4cP6/ex1/ey8hhgvY4Myjnpf+fMaTlPJ6I8R3GE0YkEBa1Gx+nmztVrIHg9zD3w25CX83/Y5BCm/hhI3b4+/M4RSkL3BTfPU6PH7/YmcSLIilqQJGB5rHZ6qSB5ed9EuW2h1yLGtFz0QzqWu/Trw8uiEjfXDrfv3/Gwd95qlT4/r3v4Nm5ff16qx6d0CDWq2CCjHvq9vMvLOgQPm1t4uQ6c0iE0yvU5d8URRtup4vdtj19PEI1PUhZ7NVto0ap2eH8+6fLrfM9/ioiXC7yXC7enC1hUD1ZfJL3cML4Z4uHcw8exBeWtGH6mH2GcG7ev02nTR2N1SD7g4Z+4rQi7PUdOKI5NXMP0SdHoDsHN3YOXIYRChItlsArTItqKmw/ogp/9laOkL2BuL/Jepgy5/tyKoYriJdREHQ7dfoYE2aYwEdeMkBZUuT2Pbxl+U2nTnzeBPZ8eN1mI7MatHsJoAzoc1uvT2ep80tXafqckwCBJeT+6nVaRlo0OXI8MmDYkWyXNm05Eye15NPV1UEuQNxdeRjhiv6kJYzV9qshionPnL/xOr0jtc1biMbcTSKy3XNxMJmWUYtxt+zifLZ4ApF/sN9+EURdefGH3iXbfyQhIC9MXfHjEiAqAlpgwjYFhegBIKH0RcVvFbZd9zgEkDGEUAJKNhY8wmodWRGhThFEgWby2C0P6MVQP0xFG0RBzRoGE0PvUu21ezkKV9nG73U8AjOdqkZ0W9M3VwN59+5PyCgli7/pESlQY1ds7+t1rNWjTrKhHVPRr/6SHH+d3QjptZxrQe5E7opqDOa53E2p1OJ5euojc3n37T3r4XCmEsJjmOM+6+RITRnVV6KJBdw7j7JiEsOk91g/p1mJeXvt+++ASRrW9+/YP6+HF5/XvcX322FWlvfv2F/TnjvQWHau6aLtt78RwPiKj3z/HPX6mMKLTppJUGT3aziVstAd909VbFYK87CleLSnbmxpFL7Q4aNAITLVefVz2RX7vvv1jpm9ziPawd9/+Ph06+/Tn1pDPG9ugN5NBQiQpjPq9KANNnTt/wUrRWRpna1lv9MVUbnhHEnLIvfpYhz3uUY90uZig+ZDNFBhtbF8sYALSh3Diwd59+0f0cGISR156mROHlDiVhD3kEQ5aOwHpMvbiC7uZd+WSXlxxkilAQKwKPm6i+ZxLEcIokB4yNH+A59Odvfv2H0vQsDyQZnuoira3JIVRXqhAffL+2PPiC7t5n7igFyp56X0IIBwH3jlzmiqnN6nrjJGkMMrqYKA2+WDqf/GF3bTuce9gUg4USCm5cd71zpnTfhcttevK+4k0VpGTtIDpsG4VEVWjacB08oH05osv7D7GM+Xe3n37e1sYnpeb4p/r+WrSoDvSGwBd0ZXPwVeYYoAEkwB6mKF5X1K5QDUxYVQPP+7SO9GE3Zh7gBWqMJgdQpVSJxiW98XrPtkSON8yYbWvDr/ya2zvvv1yUTrLvt9IEAmhrwewm5jysNnDtH7/psFY1DfAUUlcayfdPzHUHornzl8I89sDfkzoD98PXnxhN1NWWuN2dKWgF1YYOZ9NLuh79+2XBWsfGnA4QC3TzufWql3DoiRb6TJ6ZDj6jAIrTAoczlwo+8aLVk2xMTaIOqRKsnff/nGmL7WtCYMW98qx3NbHMxH23EZ5b6ao4X/bI4wCKxX3XZyHtvG8iwc6kaAVvh8QRtvW4TZfiT7tYtoe740EoM8oANyPubiA+VxN2du7bz/biRuOMAoAyfZtnj+0KbftnV7hBWI2wiga2Rjj2Wm0QpiqFcKWpO1BWU2PduV2ZfnI3n37Ga43GGEUjcQytHHu/IWhJmHg5xEeDtrTgO5HajR9gQ271R1gKi9tjs7qPr0wUBxhNAkvBjd3UGmpzjV6M/edO38hjsbabNMYnzir4W61+hnidmjvUDCHGyreK2hbul+p21aPcnN5kUBqpjjCaK+ufJnsNRfHlpbqXLNQffzc+QuRvXnPnb9wyMXNANtehmfk3PkLxlYE9c2Rm+Nr9BpxeyN50OQL1959+0+yUhjw1JZPPjs+lPfO3n37GVEwSBhh1M1dytkoA44X585fONlmw17NqkT23WTYFVIJQOfOXzjucuerUDc9SDE3Ib7X1N3H9GeGq2NrsjOV24uXU0kxKvDJRXTvvv0X2Q4UsPnZo17eO1f27tsvQ/cjVEvjF0af0QkXYc4JOHtMaeitq0HHPXzAp6W3m9uAcvLc+QtH9d7c70p16cUXdvuuUOrz7eyz/byen+qm4tXSz21zbkP8iN52d48p243q0ZSzLl8jDd+bulF8weX3cgLpuN6CdTyOXWR0FWdIrwqmTQ3wR3JNOunzfAw77yfdQL/RsH8h4hHRgglbEEcljDD6rssPSyeQjut9Y8f1Vp+R0he5V3QIdT08mZZdcWRrSQ/bn/bpeXT2XLqYtk1lK0yfJMSfO3/BTZNopYPPlXPnL8j5fiuO17u+YRnW02a8VCfd3KyMeawsDjnHsHfffueC5ew4E5Zv68+kgYSt7gciI2Ft7779YwHdpPU1+XyM+kbw6N59+3eldT/6amGEUa93KXc/5M+dv1CoupDIh/3vQjg+5wO+t4WFEKcCPqa4nUrQkN+7BhxDko15WJjTq18XI/rGo3rrQT9DY81srHpPthLA3nLxNa+38Jp3LljM1wTM4LYIljTOiG3qdwgMPIzKsN658xdO+FyJ2puQD3g3F7skeSshYVT2aacy2po3W1glXn3zZur7dNzNNA5Zhbt33/4k3YQBqOOdM6dP7d23/2hK13sMyTQd3TkgtcJaTf96ihuTn0jLEL1DP54khLzDBhxDoumpMK+n9OHJZ84BD19/mA0UgNRI6+eaaodF1aGEUb3oIY1l5YkUv+APGH5hPkVVNBgvvrD7WIoW4FU77GXeuV4YsItACiSfVEdTOIXOQRj1Sw+V7UlZc/hdpqwuDlrVDYSJj2+Cqmjg9qQskB548YXdni9EemFAkgOp3KCdMOA4gNi9c+b0gZQWjFK/gDHUpve6krUrBU3KT/gMooma46FvIEy7MI+n+SYgLnI+X3xh964UfHBP69eH74qIDqQ7E9apQR73nnfOnN4T8or+RnhP3svtdY7WdCF658zpY/o6lqabbT+vmUS9zkLfgUkCzosv7N6ph4GTNgFXLnD9L76w+7DPMORlxXEYq5M904G034DhjoIedm0liLp9M7bt8L8esjfh+fZqWldD+4OYwy2LA3Sw26nPhalBS17TB945c7r/nTOnndftmIvjLXj4/HX7fojjYm/ysbntQem29VhQ36vtvHPmtPQD3qVDaRo+3z1nJz0Nyc2/MyKXZaL+gbqvp9M30MR5EOO6TcRYq31PdZ/Eiy5aSBk5BUDveHPQQ0P6IEzr1f0ngjgfelenZqvHd6VtUZofVX09TW2sPq0vLG9FsfGB3pVlSLeDi6uVU0F/JsnN6li9FbX6WM/W+Uy1p+B46VWod3hq9Hg9f8+guDg2pY8t8vf03n37P2zyeS/nbWezldF616+LTX7c67oKiObPi/PZ9rzB2aOeE++cOe1rmtreffsPudi1bk/VjW1sIg+j1fQuL9Uf8s9HfAjOjgrTum1Q4B9e+gJ/qMFje1cvzjF6yEvfRMiH7JMt9Get5wP9HISy8YHeyvS1Gn8lP+tNdnSqTd+M9FU971F/gDujBfK+nDDlPaKDXxQ3ZxNed1+RrQ31zYRzfO/62cVFX7zrfW7J++XNuFrNNDm2aR3S4jw25zlYbVqfN1efNzqQHqzxWpPn8i0TAkRS6edpYFU/46jzhxvvvnPmdEtzwvVnQr3r31tx3LQBAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAASBul1P8Pm6H537gucvoAAAAASUVORK5CYII=";

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
var rest_url_radio = "/rddata";
var rest_url_radio_11 = "/rd11";

var rest_url_tifx_info = "/tifxinfo";

var rest_url_sacta = "/sacta";
var rest_url_extatsdest = "/extatssest";

var rest_url_versiones = "/versiones";

var rest_url_allhard = "/allhard";
var rest_url_reset = "/reset";
var rest_url_logout = "/logout";

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
    Operador: 0,
    Tecnico1: 1,
    Tecnico2: 2,
    Tecnico3: 3,
    Supervision: 4
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


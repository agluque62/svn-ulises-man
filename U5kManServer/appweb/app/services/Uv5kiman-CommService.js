/** */
angular
    .module('Uv5kiman')
    .factory('$serv', function ($q, $http, $lserv) {
        var clickCounter = 0;
        return {
            click: () =>
            {
                clickCounter += 1;
                return clickCounter;
            }
            , alive: () => {
                return safeRemoteGet("/alive");
            }
            , stdgen_get: function () {
                return safeRemoteGet(rest_url_std + "?user=" + $lserv.User());
            }
            , listinci_get: function () {
                return safeRemoteGet(rest_url_listinci);
            }
            , listinci_rec: function (data) {
                return remotePost(rest_url_listinci, data);
            }
            , cwps_get: function () {
                return safeRemoteGet(rest_url_cwps);
            }
            , cwp_version_get: function (cwp) {
                return safeRemoteGet(rest_url_cwps + "/" + cwp.name + "/version");
            }
            , gws_get: function () {
                return safeRemoteGet(rest_url_gws);
            }
            , gw_detail_get: function (gw) {
                return safeRemoteGet(rest_url_gws+"/"+gw.name);
            }
            , gw_version_get: function (gw) {
                return safeRemoteGet(rest_url_gws+"/"+gw.name,{cmd: "getVersion"});
            }
            , gw_pr_change: function(gw) {
                return remotePost(rest_url_gws+"/"+gw.name,{cmd: "chgPR"});
            }
            , exteq_get: function () {
                return safeRemoteGet(rest_url_exteq);
            }
            , pbxab_get: function () {
                return safeRemoteGet(rest_url_pbxab);
            }
            , db_inci_get: function() {    
                return safeRemoteGet(rest_url_db_inc);
            }
            , db_inci_set: function(inci) {       
                return remotePost(rest_url_db_inc, inci);         
            }
            , db_ope_get: function() {
                return safeRemoteGet(rest_url_db_ops);
            }
            , db_gw_get: function() {
                return safeRemoteGet(rest_url_db_gws);
            }
            , db_mni_get: function () {
                return safeRemoteGet(rest_url_db_mni);
            }
            , db_hist_get: function(filt) {
                return remotePost(rest_url_db_his, filt);
            }
            , db_esta_get: function (filt) {
                return remotePost(rest_url_db_est, filt);
            }
            , options_get: function () {
                return safeRemoteGet(rest_url_options);
            }
            , options_set: function (data) {
                return remotePost(rest_url_options, data);
            }
            , snmpoptions_get: function () {
                return safeRemoteGet(rest_url_snmpoptions);
            }
            , snmpoptions_set: function (data) {
                return remotePost(rest_url_snmpoptions, data);
            }
            , radio_sessions_get: function () {
                return safeRemoteGet(rest_url_radio_sessions);
            }
            , radio_gestormn_get: function () {
                return safeRemoteGet(rest_url_radio_gestormn);
            }
            , radio_hf_get: function () {
                return safeRemoteGet(rest_url_radio_hf);
            }
            , radio_hf_release: function (data) {
                var url = rest_url_radio_hf + "/" + data.id;
                return remotePost(url, data);
            }
            , radio_data: function () {
                return safeRemoteGet(rest_url_radio);
            }
            , radio_11_select: function (id) {
                return remotePost(rest_url_radio_11, { id, command: "select" });
            }
            , radio_11_enable: function (id, enable) {
                return remotePost(rest_url_radio_11, { id, command: enable });
            }
            , sacta_get: function () {
                return safeRemoteGet(rest_url_sacta);
            }
            , sacta_set: function (data) {
                return remotePost(rest_url_sacta, data);
            }
            , sacta_startstop: function (startstop) {
                return remotePost(rest_url_sacta + "/" + startstop);
            }
            , extatsdest_get: function () {
                return safeRemoteGet(rest_url_extatsdest);
            }
            , versiones_get: function () {
                return safeRemoteGet(rest_url_versiones);
            }
            , allhard_get: function () {
                return safeRemoteGet(rest_url_allhard);
            }
            , tifxinfo_get: function () {
                return safeRemoteGet(rest_url_tifx_info);
            }
            , module_reset: function () {
                return remotePost(rest_url_reset);
            }
            , logout: function () {
                return remotePost(rest_url_logout);
            }
        };

        //
        function remoteGet(url) {
            return $http.get(normalizeUrl(url),
                {
                    headers:
                    {
                        'Content-Type': 'application/json; charset=UTF-8',
                        'Click-counter': clickCounter
                    },
                    data: ''
                });
        }
        function safeRemoteGet(url) {
            var deferred = $q.defer();
            $http.get(normalizeUrl(url),
                {
                    headers: {
                        'Content-Type': 'application/json; charset=UTF-8',
                        'Click-counter': clickCounter
                    }, data: ''
                })
                .then(function (response) {
                    if (IsOk(response) == true) {
                        if (typeof response.data == 'object')
                            deferred.resolve(response);
                        else {
                            console.log("Sesion Vencida...");
                            window.location.href = "/login.html";
                        }
                    } else if (IsRedirect(response)) {
                        var newLocation = response.headers('Location');
                        console.log("safeRemoteGet " + url + " Redirect to => " + newLocation);
                        window.location.href = newLocation;
                    } else {
                        console.error("safeRemoteGet " + url + " Unknow OK response => ", response);
                        deferred.reject("Error Interno: " + response);
                    }
                }, function (response) {
                    if (IsUnauthorized(response)) {
                        console.log("safeRemoteGet " + url + " Unauthorized!");
                        window.location.href = "/login.html";
                    } else if (IsNotFound(response)) {
                        console.error("safeRemoteGet " + url + " Not Found!");
                        alertify.error($lserv.translate("Error al ejecutar la operacion") + ": " + url + " Not Found!");
                    } else if (IsLocalError(response)) {
                        console.error("safeRemoteGet " + url + " Local Error => ", response);
                        alertify.error($lserv.translate("Error al ejecutar la operacion") + ": " + url + " " + response.statusText);
                    } else if (IsServerError(response)) {
                        console.error("safeRemoteGet " + url + " Server Error => data: ", response.data);
                        window.location.href = "/error.html?error=" + response.data.code;
                    //    window.open("/error.html?error=" + response.data.code,
                    //        "Error en Servidor",
                    //        "width=600,height=600,left=(screen.width/2)-(600/2),top=(screen.height/2)-(600/2)");
                    } else {
                        console.error("safeRemoteGet " + url + " Unknow Error => ", response);
                        alertify.error($lserv.translate("Error al ejecutar la operacion") + ": " + url + " " + $lserv.translate("Error Desconocido: ") + response.statusText);
                    }
                }, function (update) {
                    console.error("safeRemoteGet " + url + " Notification => ", update);
                    deferred.reject("Notificacion: " + update);
                });
            return deferred.promise;
        }

        //
        function remotePost(url, data) {
            return $http.post(normalizeUrl(url), data);
        }

        //
        function remotePut(url, data) {
            return $http.put(normalizeUrl(url), data);
        }

        //
        function remoteDel(url) {
            return $http.delete(normalizeUrl(url));
        }

        //
        function normalizeUrl(url) {

            if (Simulate == false)
                return url;

            /** Para quitar los parametros en modo configuracion */
            var n1 = url.indexOf("?");
            var urlsim = n1 == -1 ? url : url.substr(0, n1);

            return "./simulate" + urlsim + ".json";
        }

        function IsUnauthorized(response) {
            if (response.status == 401)
                return true;
            return false;
        }
        function IsNotFound(response) {
            if (response.status == 404)
                return true;
            return false;
        }
        function IsOk(response) {
            if (response.status >= 200 && response.status < 300)
                return true;
            return false;
        }
        function IsRedirect(response) {
            if (response.status >= 300 && response.status < 400)
                return true;
            return false;
        }
        function IsLocalError(response) {
            if (response.status >= 400 && response.status < 500)
                return true;
            return false;
        }
        function IsServerError(response) {
            if (response.status >= 500)
                return true;
            return false;
        }

    });


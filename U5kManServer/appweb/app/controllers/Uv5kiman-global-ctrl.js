/** */
angular.module("Uv5kiman")
    .controller("uv5kiGlobalCtrl", function ($scope, $interval, $location, $translate, $serv, $lserv) {
        console.log("Global Controller Start...");
        /** Inicializacion */
        var firstLoad = true;
        var ctrl = this;
        ctrl.logged = null;
        ctrl.translate = $lserv.translate;

        ctrl.pagina = function (pagina) {
            var menu = $lserv.Menu(pagina);
            return menu ? menu : 0;
        };

        ctrl.date = (new Date()).toLocaleDateString();
        ctrl.hora = (new Date()).toLocaleTimeString();

        //** */
        ctrl.user = function () {
            return $lserv.logged_user();
        };

        //** */
        ctrl.retorno = () => { return true };

        //** */
        ctrl.reconoce = function (incidencia, pregunta) {
            if (pregunta) {
                alertify.confirm($lserv.translate('GCT_MSG_00') /*"¿ Desea reconocer la incidencia: " */ + incidencia.inci + "?",
                    function () {
                        $serv.listinci_rec({ user: ctrl.user(), inci: incidencia });
                        load_inci();
                    },
                    function () {
                        alertify.message($lserv.translate("Operacion Cancelada"));
                    }
                );
            }
            else {
                $serv.listinci_rec({ user: ctrl.user(), inci: incidencia });
            }
        };

        /** */
        ctrl.reconoce_todas = function () {
            alertify.confirm($lserv.translate("Desea Reconocer todas las Incidencias ?"),
                function () {
                    var data = InciTable.rows().data().toArray();
                    data.forEach(function (inci, index) {
                        ctrl.reconoce(inci, false);
                    });
                    load_inci();
                },
                function () {
                    alertify.message($lserv.translate("Operacion Cancelada"));
                }
            );
        };

        /** */
        ctrl.retorna = function () {
            $serv.logout();
        };

        /** */
        ctrl.manual = function () {
            window.open("/doc/Manual_de_Usuario.htm", "_blank");
        };

        /** */
        ctrl.about = function () {
            var page_from = parseInt(ctrl.pagina());
            // alertify.alert("Acerca de...");
            if (!alertify.About) {
                //define a new dialog
                alertify.dialog('About', function factory() {
                    return {
                        main: function (message) {
                            this.message = message;
                        },
                        setup: function () {
                            return {
                                buttons: [
                                    { text: $lserv.translate("Aceptar"), key: 27/*Esc*/ }
                                ],
                                focus: { element: 0 }
                            };
                        },
                        prepare: function () {
                            this.setContent(this.message);
                            this.set('maximizable', false);
                            this.set('closable', false);
                        },
                        build: function () {
                            this.setHeader('<p style="text-align: left; font-size: 90%;color: #4A7729;"><i><b>' + $lserv.translate('Ulises V 5000 I. Acerca de...') + '</b></i></p>');
                            this.set('resizable', true);
                        },
                        // This will be called each time an action button is clicked.
                        callback: function (closeEvent) {
                        }
                    };
                });
            }

            $serv.options_get().then(function (response) {
                var url_license = "http://" + window.location.hostname + ':' + window.location.port + '/COPYING.AUTHORIZATION.txt';
                var msg = '<div>' +
                    '<br/>' +
                    '<h4 style="color: #4A7729;">Ulises V 5000 I</h4>' +
                    '<p style="text-align:center; color: #4A7729;">Version ' + response.data.version + '</p>' +
                    '<br/>' +
                    '<p style="text-align:center; color: #4A7729;">' + $lserv.translate('INDEX_PIE') + '</p>' +
                    '<p style="text-align: right"><a href="' + url_license + '" target="_blank">' + $lserv.translate('Acuerdo de Licencia') + '</a></p>' +
                    '</div>';
                alertify.About(msg).resizeTo(500, 300).set({
                    onclose: function () {
                        ctrl.pagina(page_from);
                    }
                });
            },
                function (response) {
                    console.log(response);
                });

        };

        /** 20181009. Configuraciones locales */
        var editor = null;
        window.showOptions = function () {
            if (!alertify.LocalOptions) {
                //define a new dialog
                alertify.dialog('LocalOptions', function factory() {
                    return {
                        main: function (message) {
                            //this.message = message;
                            editor.setValue({ SoundOnClient: $lserv.Sound() });
                        },
                        setup: function () {
                            return {
                                buttons: [
                                    { text: $lserv.translate("Aceptar") },
                                    { text: $lserv.translate("Cancelar"), key: 27/*Esc*/ }
                                ],
                                focus: { element: 0 }
                            };
                        },
                        prepare: function () {
                            this.setContent(this.message);
                            this.set('maximizable', false);
                            this.set('closable', false);
                        },
                        build: function () {
                            this.setHeader('<p style="text-align: left; font-size: 90%;"><i>' + $lserv.translate('Ulises V 5000 I. Opciones Locales') + '</i></p>');
                            this.set('resizable', true);
                            editor = new JSONEditor(
                                this.elements.content,
                                {
                                    schema: {
                                        type: "object",
                                        title: "Opciones Locales",
                                        options: {
                                            disable_properties: true,
                                            disable_collapse: true,
                                            disable_edit_json: true
                                        },
                                        properties: {
                                            SoundOnClient: { title: "Sonido en Alarmas  ", type: "boolean", format: "checkbox", default: false }
                                        },
                                        required: ['SoundOnClient']
                                    },
                                    theme: 'bootstrap2'
                                }
                            );
                            this.elements.content.id = 'srv_cfg';
                        },
                        // This will be called each time an action button is clicked.
                        callback: function (closeEvent) {
                            if (closeEvent.index === 0) {
                                // TODO. Salvar las opciones....
                                if (confirm('Desea Salvar los cambios?')) {
                                    $lserv.Sound(editor.getValue().SoundOnClient);
                                }
                                else {
                                    closeEvent.cancel = true;
                                }
                            }
                        }
                    };
                });
            }
            alertify.LocalOptions("Hola").resizeTo(500, 300);
        };

        ctrl.SoundImg = function () {
            var ret = $lserv.Sound() ? "images/speakeron.png" : "images/speakeroff.png";
            return ret;
        };

        ctrl.ToggleSound = function () {
            $lserv.Sound(!$lserv.Sound());
            SoundPlayOrStop();
        };
        function SoundPlayOrStop() {
            /** Control de Audio  en Local */
            var x = document.getElementById("myAudio");
            if ($lserv.Sound() === true) {
                var data = InciTable.rows().data().toArray();
                if (data.length > 0) {
                    if (x.paused == true)
                        x.play();
                }
                else {
                    if (x.paused == false)
                        x.pause();
                }
            }
            else {
                if (x.paused == false)
                    x.pause();
            }
        }

        ctrl.PhoneGlobalState = 0;
        ctrl.PhoneStdClass = function () {
            return GlobalStdClass("btn btn-xs", ctrl.PhoneGlobalState);
        };

        ctrl.RadioGlobalState = 0;
        ctrl.RadioStdClass = function () {
            return GlobalStdClass("btn btn-xs", ctrl.RadioGlobalState);
        };
        /** Para contar los clicks... */
        ctrl.global_click = () => {
            $serv.click();
        };

        /** Nueva tabla de Incidencias */
        var RemoteData = Simulate ? "/simulate/listinci.json" : "/listinci";
        var InciTable = null;
        function InciTableInit() {
            var CfgData = {
                ajax: {
                    url: RemoteData,
                    dataSrc: "lista"
                },
                autoWidth: false,
                pageLength: 4,
                ordering: false,
                columns: [
                    { "title": $lserv.translate('INDEX_INCI_FECHA'), "data": "time", "width": "10%", "class": "text-left", "render": (data) => data },
                    { "title": $lserv.translate('INDEX_INCI_DESCRIPCION'), "data": "inci", "width": "80%", "class": "text-left", "render": (data) => data },
                    {
                        "title": $lserv.translate('INDEX_INCI_RECONOCER'), "width": "10%", "class": "text-left",
                        "render": function (data, type, full) {
                            return '<button type="button" class="btn btn-light">' + $lserv.translate("INDEX_INCI_RECONOCER") + '</button>'
                        }
                    }
                ],
                dom: "<'row'<'col-md-12 small text-info'tr>>" +
                    "<'row '<'col-md-8'><'col-md-4 text-right'p>>",
                language: {
                    "decimal": "",
                    "emptyTable": $lserv.translate("No hay datos disponibles"),
                    "info": $lserv.translate("Registros") + " _START_ - _END_ " + $lserv.translate("de") + " _TOTAL_",
                    "infoEmpty": $lserv.translate("0  Registros"),
                    "infoFiltered": "(_MAX_ )" + $lserv.translate("Registros filtrados)"),
                    "infoPostFix": "",
                    "thousands": ".",
                    "lengthMenu": $lserv.translate("Mostrar") + " _MENU_ Reg.",
                    "loadingRecords": $lserv.translate("Loading..."),
                    "processing": $lserv.translate("Processing..."),
                    "search": $lserv.translate("Buscar:"),
                    "zeroRecords": $lserv.translate("No se han encontrado registros"),
                    "paginate": {
                        first: $lserv.translate("Primera"),
                        last: $lserv.translate("Ultima"),
                        next: $lserv.translate("Siguiente"),
                        previous: $lserv.translate("Anterior")
                    },
                    aria: {
                        sortAscending: $lserv.translate(": Activar ordenado por columna ascendente"),
                        sortDescending: $lserv.translate(": Activar ordenado por columna descendente")
                    },
                    searchBuilder: {
                        add: $lserv.translate('Add Condicion'),
                        condition: $lserv.translate('Condicion'),
                        clearAll: $lserv.translate('Limpiar'),
                        deleteTitle: $lserv.translate('Borrar'),
                        data: $lserv.translate('Columna'),
                        leftTitle: 'Left',
                        logicAnd: '&',
                        logicOr: '|',
                        rightTitle: 'Right',
                        title: {
                            0: $lserv.translate('Filtro'),
                            _: $lserv.translate('Filtro (%d)')
                        },
                        value: $lserv.translate('Opcion'),
                        valueJoiner: $lserv.translate('y'),
                        conditions: {
                            string: {
                                contains: $lserv.translate('Contiene'),
                                empty: $lserv.translate('Vacio'),
                                endsWith: $lserv.translate('Acaba en '),
                                equals: $lserv.translate('Igual a'),
                                not: $lserv.translate('Distinto de '),
                                notEmpty: $lserv.translate('No Vacio'),
                                startsWith: $lserv.translate('Comienza con')
                            }
                        }
                    }
                }
            };
            InciTable = $('#dtinci').DataTable(CfgData);

            $('#dtinci tbody').on('click', 'button', function () {
                var data = InciTable.row($(this).parents('tr')).data();
                //console.log(data);
                ctrl.reconoce(data, true);
            });
        }


        ctrl.showExceptSpv = () => {
            return InciTable ? InciTable.page.info().recordsTotal > 0 && $lserv.user_access(['Spv']) : false;
        };

        /** */
        function GlobalStdClass(base, std) {
            var stdClass = std == undefined ? "btn-default" :
                std == 0 ? "btn-success" :
                    std == 1 ? "btn-warning" :
                        std == 2 ? "btn-danger" : "btn-danger";
            return base + " " + stdClass;
        }

        /** Funciones o servicios */
        /** */
        var HashCode = 0;
        function load_inci() {
            /* Obtener el estado del servidor... */
            $serv.listinci_get().then(
                function (response) {
                    if (response.status == 200 && (typeof response.data) == 'object') {
                        var data = response.data;
                        if (HashCode != data.HashCode) {
                            HashCode = data.HashCode;
                            InciTable.ajax.reload();
                            console.log('InciTable.reload');
                        }
                    }
                });
        }
        function alive() {
            $serv.alive().then(
                (response) => {
                    if ((typeof response.data) != 'object') {
                        /** El servidor me devuelve errores... */
                    console.log("Sesion Vencida...");
                    window.location.href = "/login.html";
                    }
                },
                (error) => {
                    console.log("Error Peticion alive: ", error);
                    window.location.href = "/login.html";
                }
            )
        }

        /** */
        function get_std_gen() {
            $serv.stdgen_get().then(function (response) {

                $lserv.ConfigServerHf(response.data.hf);
                $lserv.ConfigServerSacta(response.data.sct1.enable);
                $lserv.Perfil(response.data.perfil);

                if (userLang != response.data.lang) {
                    userLang = response.data.lang;
                    if (userLang.indexOf("en") == 0)
                        $translate.use('en_US');
                    else if (userLang.indexOf("fr") == 0)
                        $translate.use('fr_FR');
                    else
                        $translate.use('es_ES');
                }

                ctrl.PhoneGlobalState = response.data.tf_status;
                ctrl.RadioGlobalState = response.data.rd_status;
                //$lserv.logged_user(response.data.logged);
            }
                , function (response) {
                    console.log(response);
                });
        }

        async function StdGenGet() {
            return new Promise((resolve)=> {
                $serv.stdgen_get().then(function (response) {

                    $lserv.GlobalStd(response.data);

                    $lserv.ConfigServerHf(response.data.hf);
                    $lserv.ConfigServerSacta(response.data.sct1.enable);
                    $lserv.Perfil(response.data.perfil);

                    ctrl.PhoneGlobalState = response.data.tf_status;
                    ctrl.RadioGlobalState = response.data.rd_status;

                    if (userLang != response.data.lang) {
                        userLang = response.data.lang;

                        var useFile = userLang.indexOf("en") == 0 ? "en_US" :
                            userLang.indexOf("fr") == 0 ? "fr_FR" : "es_ES";

                        $translate.use(useFile).then(() => resolve(true), () => resolve(false));
                    }
                    else {
                        resolve(true);
                    }
                }, function (response) {
                    console.log(response);
                    resolve(false);
                });
            });
        }

        /** Funcion Periodica del controlador */
        var timer = $interval(function () {

            ctrl.date = moment().format('ll');
            ctrl.hora = moment().format('LT');

            alive();
            load_inci();
            /** Control de Audio  en Local */
            SoundPlayOrStop();
            /** Se propaga el tick */
            $scope.$broadcast(eventPolling, [1, 2, 3]);
        }, pollingTime);

        $scope.$on('$viewContentLoaded', function () {
            console.log("Global viewContentLoaded...");

            if (firstLoad == true) {
                /** Alertify */
                alertify.defaults.transition = 'zoom';
                alertify.defaults.glossary = {
                    title: $lserv.translate("ULISES V 5000 I. Aplicacion de Mantenimiento"),
                    ok: $lserv.translate("Aceptar"),
                    cancel: $lserv.translate("Cancelar")
                };
                StdGenGet().then(() => {
                    InciTableInit();
                    console.log("Global viewContentLoaded Table and Language Init...");
                    $scope.$broadcast('GlobalStarted', [1, 2, 3]);
                });
                load_inci();
            }
            else {
                $scope.$broadcast('GlobalStarted', [1, 2, 3]);
            }
            firstLoad = false;
        });

        /** Salida del Controlador. Borrado de Variables */
        $scope.$on("$destroy", function () {
            $interval.cancel(timer);
        });
        console.log("Global Controller End...");

    });



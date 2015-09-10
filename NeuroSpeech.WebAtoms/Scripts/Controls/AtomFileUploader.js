/// <reference path="AtomDockPanel.js" />

function __waFlashUploadEvent(id, name, a1,a2)
{
    var af = allControls[id];
    af.onFlashEvent(name, a1,a2);
}

window.__waFlashUploadEvent = __waFlashUploadEvent;

(function (window, baseType) {
    return classCreatorEx({
        name: "WebAtoms.AtomFileUploader",
        base: baseType,
        start: function () {
            this._presenters = ["viewStack", "filePresenter", "flashPresenter", "flashButtonPresenter"];
        },
        properties: {
            url: null,
            fileTypes: null,
            filters: null,
            value: null,
            uploadOnUrlSet: false,
            progress: 0,
            lockProgress: true,
            state: '',
            error: '',
            displayMode: 0,
            processUrl: '',
            processData: null,
            processMaxTries: 100,
            processInterval: 5000,
            proccessResult: {},
            processDone: false,
            onSelect: null,
            accept: "*/*",
            capture: ""
        },
        methods: {
            set_accept: function (v) {
                this._accept = v;
                if (v) {
                    if (this._filePresenter) {
                        $(this._filePresenter).attr("accept", v);
                    }
                }
            },

            set_capture: function (v) {
                this._capture = v;
                if (v) {
                    if (this._filePresenter) {
                        $(this._filePresenter).attr("capture", v);
                    }
                }
            },
            set_progress: function (v) {
                this._progress = v;
                if (this._lockProgress && v) {
                    if (v > 99) {
                        AtomBinder.setValue(atomApplication, "busyMessage", "Converting...");
                    }
                    AtomBinder.setValue(atomApplication, "progress", v);
                }
            },
            set_value: function (v) {
                this._value = v;
                var caller = this;
                this.dispatcher.callLater(function () {
                    if (caller._value) {
                        if (caller._onSelect) {
                            caller.invokeAction(caller._onSelect);
                        }
                    }
                });
            },
            set_url: function (v) {
                this._url = v;
                if (!v)
                    return;
                if (this._uploadOnUrlSet) {
                    this.uploadFile();
                }

            },

            isFlashSupported: function () {
                return AtomBrowser.supportsFlash && !AtomBrowser.supportsUpload;
            },

            onFileSelected: function (f) {
                this._files = f.target.files;
                AtomBinder.setValue(this, "value", this._files[0].name);
            },


            uploadFile: function () {
                if (!this._url) {

                    // process next???
                    return;
                }

                if (this._isFlash) {
                    var fp = this.get_player();
                    var r = fp.upload(this._url);
                } else {
                    var xhr = this._xhr;
                    if (!xhr) {
                        xhr = new XMLHttpRequest();
                        var upload = xhr.upload;
                        try {
                            xhr.timeout = 3600000;
                        } catch (e) {
                            // IE 10 has some issue with this code..
                        }

                        this.bindEvent(upload, "progress", "onProgress");
                        this.bindEvent(upload, "timeout", "onError");
                        this.bindEvent(upload, "error", "onError");
                        this.bindEvent(xhr, "load", "onComplete");
                        this.bindEvent(xhr, "error", "onError");
                        this.bindEvent(xhr, "timeout", "onError");
                        this._xhr = xhr;
                    }

                    var fd = new FormData();
                    fd.append("fileToUpload", this._files[0]);

                    xhr.open("POST", this._url);
                    //xhr.setRequestHeader("Content-Type", "multipart/form-data");
                    xhr.send(fd);

                }

                if (this._lockProgress) {
                    atomApplication.setBusy(true, "Uploading...");
                }

                AtomBinder.setValue(this, "displayMode", 2);
                AtomBinder.setValue(this, "state", "uploading");
            },

            invokeNext: function () {
                if (this._next) {
                    if (this._isFlash) {
                        var fp = this.get_player();
                        fp.reset();
                        AtomBinder.setValue(this, "displayMode", 1);
                    } else {
                        AtomBinder.setValue(this, "displayMode", 0);
                    }
                    AtomBinder.setValue(this, "value", "");
                    this.invokeAction(this._next);
                }
            },

            refresh: function () {
                if (this.get_value()) {
                    this.uploadFile();
                } else {
                    this.invokeNext();
                }
            },

            onUploadFinished: function () {
                AtomBinder.setValue(this, "displayMode", 4);
                AtomBinder.setValue(this, "state", "uploaded");
                if (this._lockProgress) {
                    AtomBinder.setValue(atomApplication, "progress", 0);
                    atomApplication.setBusy(false, "Uploading...");
                }

                if (this._processUrl) {
                    var caller = this;
                    this.tries = this._processMaxTries;
                    if (this.tries && this.tries.constructor == String)
                        this.tries = parseInt(this.tries, 10);
                    this.timer = setInterval(function () {
                        caller.queryProcessUrl();
                    }, this._processInterval);
                    atomApplication.setBusy(true, "Converting...");
                } else {
                    this.invokeNext();
                }
            },

            queryProcessUrl: function () {
                var caller = this;
                var res = function (r) { caller.onQueryResult(r); };

                var data = this._processData;

                this.invokeAjax(this._processUrl, { success: res, error: res, data: data });
            },

            onQueryResult: function (r) {
                AtomBinder.setValue(this, "processResult", r);

                this.tries = this.tries - 1;
                if (this.tries <= 0 || this._processDone) {
                    // stop..

                    var caller = this;
                    clearInterval(this.timer);
                    delete this.timer;

                    setTimeout(function () {
                        atomApplication.setBusy(false, "Converting...");
                        caller.invokeNext();
                    }, caller._processInterval);

                }
            },

            onError: function (evt) {
                this._lastError = evt;
                AtomBinder.setValue(this, "displayMode", 3);
                AtomBinder.setValue(this, "state", "error");
            },
            onProgress: function (evt) {
                //evt = evt.originalEvent;
                if (evt.lengthComputable) {
                    var percentComplete = Math.round(evt.loaded * 100 / evt.total);
                    AtomBinder.setValue(this, "progress", percentComplete);
                }
            },
            onComplete: function (evt) {
                var result = null;
                if (evt.target) {
                    result = evt.target.responseText;
                } else {
                    result = evt.result;
                }
                this.onUploadFinished();
            },

            get_player: function () {
                var appName = this._fpID;
                if (navigator.appName.indexOf("Microsoft") != -1) {
                    return window[appName];
                } else {
                    return document[appName];
                }
            },



            playerUpdated: function () {
                // load status..
                if (!this._playerReady) {
                    this._playerReady = true;
                    var fp = this.get_player();
                    fp.setProperty("filters", JSON.stringify(this._filters));
                    if (this._uploadOnUrlSet && this._url) {
                        fp.uploadFile(this._url);
                    }
                }
                var p = this.get_player();
                AtomBinder.setValue(this, "state", p.getProperty("currentState").toLowerCase());
                if (this._state == 'uploaded') {
                    // yes.. do next...
                    this.invokeAction(this.get_next());
                }
                if (this._state == 'ready') {
                    AtomBinder.setValue(this, "value", "selected");
                }
            },

            onFlashEvent: function (name, a1, a2) {
                switch (name) {
                    case "playerReady":
                        var ft = AtomBinder.getValue(this, "fileTypes");
                        if (ft) {
                            var fp = this.get_player();
                            fp.clearFileTypes();
                            var ae = new AtomEnumerator(ft);
                            while (ae.next()) {
                                var fti = ae.current();
                                fp.addFileType(fti.description, fti.extensions);
                            }
                        }
                        break;
                    case "progress":
                        this.onFlashProgress(a1, a2);
                        break;
                    case "complete":
                        this.onFlashSuccess(null);
                        break;
                    case "selected":
                        AtomBinder.setValue(this, "value", a1);
                        break;
                    case "httpError":
                    case "ioError":
                    case "securityError":
                        this.onFlashError(name, a1);
                        break;
                    default:
                }
            },

            onFlashProgress: function (bytesLoaded, bytesTotal) {
                var percent = Math.ceil((bytesLoaded / bytesTotal) * 100);
                AtomBinder.setValue(this, "progress", percent);
            },
            onFlashError: function (errorCode, message) {
                this._lastError = { file: file, errorCode: errorCode, message: message };
                AtomBinder.setValue(this, "displayMode", 3);
                AtomBinder.setValue(this, "state", "error");
            },
            onFlashSuccess: function (serverData) {
                this.onUploadFinished();
            },
            onFlashDialogComplete: function (numFilesSelected, numFilesQueued) {
                if (numFilesQueued > 0)
                    AtomBinder.setValue(this, "value", "selected");
            },

            onFlashLoaded: function () {
                this.swfUploader.setButtonDimensions(100, 25);
            },

            useFlash: function () {
                if (AtomBrowser.isIE && AtomBrowser.majorVersion <= 9)
                    return true;
                if (AtomBrowser.isSafari) {

                    if (AtomBrowser.isMac && AtomBrowser.majorVersion < 6) {
                        return true;
                    }
                }
                return false;
            },

            initialize: function () {
                var element = this._element;

                allControls[element.id] = this;

                baseType.initialize.apply(this, arguments);
                element.style.height = "25px";
                element.style.minWidth = "240px";
                if (this.useFlash()) {
                    AtomBinder.setValue(this, "displayMode", 1);
                    var fp = this._flashPresenter;
                    AtomUI.assignID(fp);
                    this._fpID = fp.id + "_flash";

                    createFlashUploader(element.id, fp.id, this._fpID);
                    this._isFlash = true;
                } else {
                    AtomBinder.setValue(this, "displayMode", 0);
                    var f = this._filePresenter;
                    this.bindEvent(f, "change", "onFileSelected");
                    this.clickCommand = function () {
                        $(f).trigger('click');
                    };
                }

            }
        }
    });
})(window, WebAtoms.AtomDockPanel.prototype);



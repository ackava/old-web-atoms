/// <reference path="AtomPlayer.js" />

(function (baseType) {
    return classCreatorEx({
        name: "WebAtoms.AtomVideoPlayer",
        base: baseType,
        start: function () {
            this._isFlash = false;
            this._fpID = null;
            this._divID = null;
            this._playerReady = false;
            this._timeoutCallback = null;
            this._playerID = null;
            this._srcID = null;
            this._video = null;
            this._src = null;
            this._errorHandler = null;
            this._autoCreate = false;
            this._playerCreated = false;
        },
        properties: {
            swfPath: "/content/player.swf",
            autoPlay: true,
            poster: null,
            autoCreate: false
        },
        methods: {
            set_poster: function (v) {
                this._poster = v;
                if (this._isFlash) {
                    jwplayer(this._divID).load({ image: v });
                }
                else {
                    if (this._video) {
                        $(this._video).attr("poster", this._poster);
                    }
                }
            },

            onError: function () {
                Atom.alert("something went wrong with video");
            },

            get_player: function (appName) {
                if (navigator.appName.indexOf("Microsoft") != -1) {
                    return window[appName];
                } else {
                    return document[appName];
                }
            },

            playerUpdated: function () {
                this._playerReady = true;
            },

            playIfQueued: function () {
                if (this._queuePlay) {
                    this.play(this._url);
                }
            },

            onUpdateUI: function () {
                baseType.onUpdateUI.call(this);

                var s = $(this._element).css('visibility');
                if (s == "visible") {
                    this.playIfQueued();
                }
            },

            set_options: function (v) {
                if (!v)
                    return;

                this._poster = v.poster;
                if (v.autoPlay !== undefined) {
                    this._autoPlay = v.autoPlay;
                }
                if (v.url !== undefined) {
                    this._url = v.url;
                    this.play(v.url);
                }
            },
            get_options: function () {
                return {};
            },

            play: function (url) {

                if (!url) {
                    this.stop();
                    return;
                }

                this._url = url;

                var s = $(this._element).css('visibility');
                if (s != "visible") {
                    if (!this._queuePlay) {
                        this._queuePlay = true;
                    }
                    return;
                }

                this._queuePlay = false;


                var element = this.get_element();
                this.create();
                if (this._isFlash) {
                    jwplayer(this._divID).load({ file: url, image: this._poster });
                    if (this._autoPlay) {
                        jwplayer(this._divID).play();
                    }
                    return;
                } else {

                    if (this._video) {
                        $(this._video).remove();
                    }

                    this._video = document.createElement("video");
                    this._playerID = this._divID + "_Video";
                    this._video.id = this._playerID;
                    $(this._video).attr("controls", "controls");
                    this._video.style.width = "100%";
                    this._video.style.height = "100%";
                    this._video.autobuffer = true;
                    this._video.autoplay = true;
                    this._video.allowfullscreen = true;
                    if (this._poster) {
                        $(this._video).attr("poster", this._poster);
                    }
                    var src = document.createElement("source");
                    src.type = "video/mp4";
                    src.src = url;
                    this._video.appendChild(src);
                    $(element).append(this._video);
                    this._video.play();
                }
            },

            stop: function () {
                if (!this._playerCreated)
                    return;
                var element = this.get_element();
                if (this._isFlash) {
                    jwplayer(this._divID).pause(true);
                } else {
                    if (this._video) {
                        this._video.pause();
                    }
                }
            },

            supportsMP4: function () {
                var ua = window.navigator.userAgent.toLowerCase();

                if (/msie 10.0|safari|android|iphone|ipad|ios/gi.test(ua)) {
                    if (/chrome/gi.test(ua) && /mac|windows/gi.test(ua)) {
                        return false;
                    }
                    return true;
                }
                return false;
            },

            create: function () {
                if (this._playerCreated)
                    return;
                this._playerCreated = true;
                var element = this.get_element();
                AtomUI.assignID(element);
                allControls[element.id] = this;


                this._divID = element.id + "_Player";


                if (!this.supportsMP4()) {

                    var childDiv = document.createElement("div");
                    childDiv.id = this._divID;

                    $(element).append(childDiv);

                    this._fpID = this._divID + "_Flash";
                    //createVideoPlayer(element.id, this._divID, this._fpID);
                    jwplayer(this._divID).setup({
                        'flashplayer': this._swfPath,
                        'id': this._fpID,
                        'bufferlength': 10,
                        'controlbar.position': 'bottom',
                        'width': '100%',
                        'height': '100%',
                        'events': {
                            'onMeta': function (m) {
                                //log(m);
                            }
                        }
                    });
                    this._isFlash = true;
                }
            },

            initialize: function () {
                var element = this.get_element();
                $(element).addClass("atom-video-player");
                if (this._autoCreate) {
                    this.create();
                }
                baseType.initialize.call(this);
            }
        }
    });
})(WebAtoms.AtomPlayer.prototype);

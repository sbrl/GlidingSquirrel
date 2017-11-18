#!/usr/bin/env bash

if [ ! -d autobahn ]; then
	mkdir autobahn;
	virtualenv autobahn;
	source autobahn/bin/activate;
	pip install autobahntestsuite;
fi

source autobahn/bin/activate;
wstest -m fuzzingclient;

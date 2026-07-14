{{/*
Expand the name of the chart.
*/}}
{{- define "citizen-service.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
*/}}
{{- define "citizen-service.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart label.
*/}}
{{- define "citizen-service.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels.
*/}}
{{- define "citizen-service.labels" -}}
helm.sh/chart: {{ include "citizen-service.chart" . }}
{{ include "citizen-service.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels — API.
*/}}
{{- define "citizen-service.selectorLabels" -}}
app.kubernetes.io/name: {{ include "citizen-service.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
API component selector labels.
*/}}
{{- define "citizen-service.api.selectorLabels" -}}
app.kubernetes.io/name: {{ include "citizen-service.name" . }}-api
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Web component selector labels.
*/}}
{{- define "citizen-service.web.selectorLabels" -}}
app.kubernetes.io/name: {{ include "citizen-service.name" . }}-web
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
API image tag — falls back to chart appVersion.
*/}}
{{- define "citizen-service.api.image" -}}
{{ .Values.api.image.repository }}:{{ .Values.api.image.tag | default .Chart.AppVersion }}
{{- end }}

{{/*
Web image tag — falls back to chart appVersion.
*/}}
{{- define "citizen-service.web.image" -}}
{{ .Values.web.image.repository }}:{{ .Values.web.image.tag | default .Chart.AppVersion }}
{{- end }}

{{/*
Name of the secret to mount (existing or chart-managed).
*/}}
{{- define "citizen-service.secretName" -}}
{{- if .Values.secret.existingSecret }}
{{- .Values.secret.existingSecret }}
{{- else }}
{{- include "citizen-service.fullname" . }}-secret
{{- end }}
{{- end }}

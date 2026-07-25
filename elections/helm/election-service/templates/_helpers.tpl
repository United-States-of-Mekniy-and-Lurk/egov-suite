{{- define "election-service.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "election-service.fullname" -}}
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

{{- define "election-service.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "election-service.labels" -}}
helm.sh/chart: {{ include "election-service.chart" . }}
{{ include "election-service.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{- define "election-service.selectorLabels" -}}
app.kubernetes.io/name: {{ include "election-service.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{- define "election-service.api.selectorLabels" -}}
app.kubernetes.io/name: {{ include "election-service.name" . }}-api
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{- define "election-service.web.selectorLabels" -}}
app.kubernetes.io/name: {{ include "election-service.name" . }}-web
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{- define "election-service.api.image" -}}
{{ .Values.api.image.repository }}:{{ .Values.api.image.tag | default .Chart.AppVersion }}
{{- end }}

{{- define "election-service.web.image" -}}
{{ .Values.web.image.repository }}:{{ .Values.web.image.tag | default .Chart.AppVersion }}
{{- end }}

{{- define "election-service.secretName" -}}
{{- if .Values.secret.existingSecret }}
{{- .Values.secret.existingSecret }}
{{- else }}
{{- include "election-service.fullname" . }}-secret
{{- end }}
{{- end }}

{{- define "election-service.postgresServiceName" -}}
{{- printf "%s-postgres" (include "election-service.fullname" .) }}
{{- end }}

{{- define "election-service.postgresSecretName" -}}
{{- if .Values.postgresql.existingSecret }}
{{- .Values.postgresql.existingSecret }}
{{- else }}
{{- printf "%s-postgres-secret" (include "election-service.fullname" .) }}
{{- end }}
{{- end }}

{{- define "election-service.connectionString" -}}
{{- if .Values.secret.connectionString }}
{{- .Values.secret.connectionString }}
{{- else if .Values.postgresql.enabled }}
{{- printf "Host=%s;Database=%s;Username=%s;Password=%s" (include "election-service.postgresServiceName" .) .Values.postgresql.database .Values.postgresql.username .Values.postgresql.password }}
{{- else }}
CHANGEME_CONNECTION_STRING
{{- end }}
{{- end }}